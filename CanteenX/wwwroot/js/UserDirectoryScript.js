(() => {
    "use strict";

    const endpoint = window.userDirectoryConfig?.endpoint;

    const elements = {
        search: document.getElementById("udSearch"),
        status: document.getElementById("udStatusFilter"),
        role: document.getElementById("udRoleFilter"),
        pageSize: document.getElementById("udPageSize"),
        table: document.getElementById("udTable"),
        tbody: document.getElementById("udTableBody"),
        empty: document.getElementById("udEmpty"),
        pagination: document.getElementById("udPagination"),
        resultText: document.getElementById("udResultText"),
        selectAll: document.getElementById("udSelectAll"),
        bulkbar: document.getElementById("udBulkbar"),
        selectedCount: document.getElementById("udSelectedCount"),
        error: document.getElementById("udError"),
        errorMessage: document.getElementById("udErrorMessage"),
        retry: document.getElementById("udRetry"),
        export: document.getElementById("udExport"),
        exportSelected: document.getElementById("udExportSelected"),
        modal: document.getElementById("udUserModal"),
        modalTitle: document.getElementById("udModalTitle"),
        modalBody: document.getElementById("udModalBody")
    };

    const state = {
        pageNumber: 1,
        pageSize: Number(elements.pageSize?.value) || 10,
        searchTerm: "",
        status: "",
        role: "",
        totalRecords: 0,
        totalPages: 0,
        items: [],
        selectedIds: new Set(),
        requestController: null,
        requestNumber: 0,
        searchTimer: null
    };

    const roleLabels = {
        Admin: "Admin",
        Student: "Student",
        CanteenStaff: "Canteen Staff"
    };

    function normalizeRole(role) {
        return roleLabels[role] ? roleLabels[role] : (role || "Unknown");
    }

    function getStatus(user) {
        return user.isActive === true ? "Active" : "Inactive";
    }

    function formatDate(value) {
        if (!value) return "—";

        const date = new Date(value);
        if (Number.isNaN(date.getTime())) return "—";

        return date.toLocaleDateString("en-GB", {
            day: "2-digit",
            month: "short",
            year: "numeric"
        });
    }

    function createElement(tag, className, text) {
        const element = document.createElement(tag);

        if (className) element.className = className;
        if (text !== undefined) element.textContent = text;

        return element;
    }

    function createButton(label, className, onClick, disabled = false) {
        const button = createElement("button", className, label);
        button.type = "button";
        button.disabled = disabled;
        button.addEventListener("click", onClick);
        return button;
    }

    function showError(message) {
        if (!elements.errorMessage || !elements.error) return;
        elements.errorMessage.textContent = message;
        elements.error.classList.remove("d-none");
    }

    function hideError() {
        if (!elements.error) return;
        elements.error.classList.add("d-none");
    }

    // Shared helper to navigate to Edit page with all user query params
    function navigateToEditUser(user) {
        const userId = String(user?.id ?? "").trim();
        const editUrl = window.userDirectoryConfig?.editUrl;

        if (!editUrl) {
            showError("Edit page URL is not configured.");
            return;
        }

        if (!userId) {
            showError("Unable to identify the selected user.");
            return;
        }

        const params = new URLSearchParams({
            id: userId,
            fullName: String(user.fullName ?? ""),
            email: String(user.email ?? ""),
            role: String(user.role ?? ""),
            isActive: String(Boolean(user.isActive))
        });

        const separator = editUrl.includes("?") ? "&" : "?";
        window.location.href = `${editUrl}${separator}${params.toString()}`;
    }

    function showLoading() {
        elements.tbody.replaceChildren();

        const row = createElement("tr");
        const cell = createElement("td", "text-center py-5");
        cell.colSpan = 7;

        const spinner = createElement(
            "span",
            "spinner-border spinner-border-sm me-2"
        );

        spinner.setAttribute("role", "status");
        spinner.setAttribute("aria-hidden", "true");

        cell.append(spinner, document.createTextNode("Loading users..."));
        row.appendChild(cell);
        elements.tbody.appendChild(row);
    }

    async function loadUsers(pageNumber = state.pageNumber) {
        if (!endpoint) {
            showError("User Directory endpoint is not configured. Check UserDirectory.cshtml.");
            return;
        }

        state.pageNumber = Math.max(1, pageNumber);

        if (state.requestController) {
            state.requestController.abort();
        }

        const controller = new AbortController();
        state.requestController = controller;

        const requestNumber = ++state.requestNumber;

        const query = new URLSearchParams({
            pageNumber: String(state.pageNumber),
            pageSize: String(state.pageSize),
            searchTerm: state.searchTerm,
            status: state.status,
            role: state.role
        });

        hideError();
        showLoading();

        elements.empty.classList.add("d-none");
        elements.table.classList.remove("d-none");

        try {
            const response = await fetch(`${endpoint}?${query.toString()}`, {
                method: "GET",
                headers: { "Accept": "application/json" },
                credentials: "same-origin",
                signal: controller.signal
            });

            if (requestNumber !== state.requestNumber) return;

            if (response.status === 401) {
                throw new Error("Your session has expired. Please sign in again.");
            }

            if (response.status === 403) {
                throw new Error("You do not have permission to view the user directory.");
            }

            if (!response.ok) {
                throw new Error(`Unable to retrieve users (HTTP ${response.status}).`);
            }

            const responseData = await response.json();

            if (responseData?.success === false) {
                throw new Error(responseData.message || "Unable to retrieve users.");
            }

            const data = responseData?.data ?? responseData;

            if (!data || !Array.isArray(data.items)) {
                throw new Error(
                    responseData?.message ||
                    "The server returned an unexpected response. Expected a users array at data.items."
                );
            }

            if (requestNumber !== state.requestNumber) return;

            state.items = data.items;
            state.pageNumber = Number(data.pageNumber ?? data.pagination?.pageNumber) || state.pageNumber;
            state.pageSize = Number(data.pageSize ?? data.pagination?.pageSize) || state.pageSize;
            state.totalRecords = Number(data.totalRecords ?? data.pagination?.totalRecords) || 0;
            state.totalPages = Number(data.totalPages ?? data.pagination?.totalPages) || 0;

            elements.pageSize.value = String(state.pageSize);

            const visibleIds = new Set(state.items.map(user => String(user.id)));

            for (const id of state.selectedIds) {
                if (!visibleIds.has(id)) {
                    state.selectedIds.delete(id);
                }
            }

            renderUsers();
            renderPagination();
            renderSummary();
            updateBulkbar();
        } catch (error) {
            if (error.name === "AbortError") return;

            state.items = [];
            state.totalRecords = 0;
            state.totalPages = 0;

            elements.tbody.replaceChildren();
            elements.pagination.replaceChildren();
            elements.resultText.textContent = "Unable to load users.";

            showError(error.message || "An unexpected error occurred.");
            updateBulkbar();
        } finally {
            if (requestNumber === state.requestNumber) {
                state.requestController = null;
            }
        }
    }

    function renderUsers() {
        elements.tbody.replaceChildren();

        if (state.items.length === 0) {
            elements.table.classList.add("d-none");
            elements.empty.classList.remove("d-none");
            elements.selectAll.checked = false;
            return;
        }

        elements.table.classList.remove("d-none");
        elements.empty.classList.add("d-none");

        const fragment = document.createDocumentFragment();

        for (const user of state.items) {
            const id = String(user.id ?? "");
            const name = String(user.fullName || "Unnamed user");
            const email = String(user.email || "—");
            const role = String(user.role || "Unknown");
            const status = getStatus(user);

            const row = createElement("tr", "ud-row");
            row.dataset.id = id;
            row.dataset.name = name;
            row.dataset.email = email;
            row.dataset.role = role;
            row.dataset.status = status;
            row.dataset.createdAt = formatDate(user.createdAt);

            // Selection checkbox
            const checkCell = createElement("td");
            const checkbox = createElement("input", "ud-check ud-row-check");
            checkbox.type = "checkbox";
            checkbox.checked = state.selectedIds.has(id);
            checkbox.setAttribute("aria-label", `Select ${name}`);

            checkbox.addEventListener("change", () => {
                if (checkbox.checked) {
                    state.selectedIds.add(id);
                } else {
                    state.selectedIds.delete(id);
                }

                updateBulkbar();
                updateSelectAll();
            });

            checkCell.appendChild(checkbox);

            // User identity
            const userCell = createElement("td");
            const userContainer = createElement("div", "ud-user");
            const avatar = createElement("div", "ud-avatar",
                name.trim().charAt(0).toUpperCase() || "?");

            const identity = createElement("div");
            identity.appendChild(createElement("span", "ud-user-name", name));
            identity.appendChild(createElement("span", "ud-user-id", `ID: ${id}`));

            userContainer.append(avatar, identity);
            userCell.appendChild(userContainer);

            // Email
            const emailCell = createElement("td", "ud-email", email);

            // Role
            const roleCell = createElement("td");
            const roleClass = role === "Admin"
                ? "ud-role-admin"
                : role === "CanteenStaff"
                    ? "ud-role-staff"
                    : "ud-role-student";

            roleCell.appendChild(
                createElement("span", `ud-badge ${roleClass}`, normalizeRole(role))
            );

            // Status
            const statusCell = createElement("td");
            const statusClass = status === "Active"
                ? "ud-status-active"
                : "ud-status-inactive";

            const statusBadge = createElement("span", `ud-badge ${statusClass}`);
            const statusIcon = createElement(
                "i",
                `bi ${status === "Active" ? "bi-check-circle-fill" : "bi-dash-circle-fill"} me-1`
            );

            statusBadge.append(statusIcon, document.createTextNode(status));
            statusCell.appendChild(statusBadge);

            // Joined Date
            const joinedDate = formatDate(user.createdAt);
            const joinedDateCell = createElement("td", "ud-lastactive", joinedDate);
            joinedDateCell.title = user.createdAt
                ? `Joined: ${user.createdAt}`
                : "Joined date is not available.";

            // Actions
            const actionCell = createElement("td", "text-end");
            const dropdown = createElement("div", "dropdown");

            const menuButton = createElement("button", "ud-action-btn");
            menuButton.type = "button";
            menuButton.dataset.bsToggle = "dropdown";
            menuButton.setAttribute("aria-expanded", "false");
            menuButton.setAttribute("aria-label", `Actions for ${name}`);
            menuButton.appendChild(createElement("i", "bi bi-three-dots"));

            const menu = createElement("ul", "dropdown-menu dropdown-menu-end ud-dropdown");

            const viewItem = createElement("li");
            const viewButton = createElement("button", "dropdown-item");
            viewButton.type = "button";
            viewButton.dataset.action = "view";
            viewButton.append(
                createElement("i", "bi bi-person-vcard text-primary me-2"),
                document.createTextNode("View Profile")
            );
            viewItem.appendChild(viewButton);

            const editItem = createElement("li");
            const editButton = createElement("button", "dropdown-item");
            editButton.type = "button";
            editButton.dataset.action = "edit";
            editButton.append(
                createElement("i", "bi bi-pencil-square text-warning me-2"),
                document.createTextNode("Edit Details")
            );
            editItem.appendChild(editButton);

            const deleteItem = createElement("li");
            const deleteButton = createElement("button", "dropdown-item text-danger");
            deleteButton.type = "button";
            deleteButton.dataset.action = "delete";
            deleteButton.append(
                createElement("i", "bi bi-trash text-danger me-2"),
                document.createTextNode("Delete User")
            );
            deleteItem.appendChild(deleteButton);

            menu.append(viewItem, editItem, deleteItem);
            dropdown.append(menuButton, menu);
            actionCell.appendChild(dropdown);

            row.append(
                checkCell,
                userCell,
                emailCell,
                roleCell,
                statusCell,
                joinedDateCell,
                actionCell
            );

            fragment.appendChild(row);
        }

        elements.tbody.appendChild(fragment);
        updateSelectAll();
    }

    function renderSummary() {
        if (state.totalRecords === 0) {
            elements.resultText.textContent = "Showing 0 users";
            return;
        }

        const start = ((state.pageNumber - 1) * state.pageSize) + 1;
        const end = Math.min(
            state.pageNumber * state.pageSize,
            state.totalRecords
        );

        elements.resultText.textContent =
            `Showing ${start}–${end} of ${state.totalRecords} users`;
    }

    function renderPagination() {
        elements.pagination.replaceChildren();

        const totalPages = state.totalPages;
        const current = state.pageNumber;

        if (totalPages <= 1) return;

        elements.pagination.appendChild(
            createButton(
                "Previous",
                "ud-page-btn",
                () => loadUsers(current - 1),
                current <= 1
            )
        );

        const pages = new Set([1, totalPages]);

        for (let page = current - 2; page <= current + 2; page++) {
            if (page >= 1 && page <= totalPages) pages.add(page);
        }

        let previousPage = 0;

        for (const page of [...pages].sort((a, b) => a - b)) {
            if (previousPage && page - previousPage > 1) {
                const dots = createElement("span", "px-2", "…");
                elements.pagination.appendChild(dots);
            }

            const button = createButton(
                String(page),
                `ud-page-btn${page === current ? " active" : ""}`,
                () => loadUsers(page),
                page === current
            );

            if (page === current) {
                button.setAttribute("aria-current", "page");
                button.setAttribute("aria-label", `Page ${page}, current page`);
            } else {
                button.setAttribute("aria-label", `Go to page ${page}`);
            }

            elements.pagination.appendChild(button);
            previousPage = page;
        }

        elements.pagination.appendChild(
            createButton(
                "Next",
                "ud-page-btn",
                () => loadUsers(current + 1),
                current >= totalPages
            )
        );
    }

    function updateSelectAll() {
        const checkboxes = [
            ...elements.tbody.querySelectorAll(".ud-row-check")
        ];

        elements.selectAll.checked =
            checkboxes.length > 0 &&
            checkboxes.every(checkbox => checkbox.checked);

        elements.selectAll.indeterminate =
            checkboxes.some(checkbox => checkbox.checked) &&
            !checkboxes.every(checkbox => checkbox.checked);
    }

    function updateBulkbar() {
        const selectedVisible = state.items.filter(user =>
            state.selectedIds.has(String(user.id))
        );

        elements.selectedCount.textContent = String(state.selectedIds.size);
        elements.bulkbar.hidden = state.selectedIds.size === 0;
        elements.exportSelected.disabled = selectedVisible.length === 0;
    }

    function csvEscape(value) {
        let text = String(value ?? "");

        if (/^[\s]*[=+\-@]/.test(text)) {
            text = "'" + text;
        }

        return '"' + text.replace(/"/g, '""') + '"';
    }

    function exportRows(users, filename) {
        if (!users.length) {
            showError("There are no users to export.");
            return;
        }

        const header = [
            "User ID", "Name", "Email", "Role",
            "Status", "Last Active", "Joined Date"
        ];

        const lines = [
            header,
            ...users.map(user => [
                user.id,
                user.fullName,
                user.email,
                normalizeRole(user.role),
                getStatus(user),
                "",
                formatDate(user.createdAt)
            ])
        ];

        const csv = "\uFEFF" +
            lines.map(line => line.map(csvEscape).join(",")).join("\r\n");

        const blob = new Blob([csv], {
            type: "text/csv;charset=utf-8;"
        });

        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");

        link.href = url;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        link.remove();

        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }

    // =========================================================
    // PREMIUM iOS USER PROFILE MODAL (Contact Card Style)
    // =========================================================
    function appendIosRow(list, iconClass, iconBg, label, value, isLast = false) {
        const item = createElement(
            "div",
            `d-flex align-items-center justify-content-between py-3 px-3 ${isLast ? "" : "border-bottom"}`
        );
        item.style.borderColor = "rgba(60, 60, 67, 0.08)";

        const left = createElement("div", "d-flex align-items-center gap-3");
        const iconBox = createElement(
            "div",
            "d-flex align-items-center justify-content-center rounded-3",
            ""
        );
        iconBox.style.width = "34px";
        iconBox.style.height = "34px";
        iconBox.style.background = iconBg;
        iconBox.appendChild(createElement("i", `${iconClass} fs-6`));

        const labelEl = createElement("span", "fw-medium text-secondary small", label);
        left.append(iconBox, labelEl);

        const right = createElement(
            "span",
            "fw-semibold text-dark small text-end text-break ms-3",
            value || "—"
        );

        item.append(left, right);
        list.appendChild(item);
    }

    function showProfile(user) {
        elements.modalTitle.textContent = "Contact Card";
        elements.modalBody.replaceChildren();

        const name = String(user.fullName || "Unnamed user");
        const initial = name.trim().charAt(0).toUpperCase() || "?";
        const roleLabel = normalizeRole(user.role);
        const status = getStatus(user);
        const isActive = status === "Active";

        // 1. iOS Hero Header
        const hero = createElement("div", "text-center pb-4 pt-2");

        const avatarWrap = createElement("div", "position-relative d-inline-block mb-3");
        const avatar = createElement(
            "div",
            "d-flex align-items-center justify-content-center fw-bold text-white shadow",
            initial
        );
        avatar.style.width = "76px";
        avatar.style.height = "76px";
        avatar.style.fontSize = "1.85rem";
        avatar.style.borderRadius = "24px";
        avatar.style.background = "linear-gradient(135deg, #e8281a 0%, #ff6b5b 100%)";
        avatar.style.boxShadow = "0 12px 24px rgba(232, 40, 26, 0.25)";

        const statusDot = createElement("span", "position-absolute");
        statusDot.style.width = "16px";
        statusDot.style.height = "16px";
        statusDot.style.borderRadius = "50%";
        statusDot.style.bottom = "2px";
        statusDot.style.right = "2px";
        statusDot.style.border = "2.5px solid #ffffff";
        statusDot.style.backgroundColor = isActive ? "#34c759" : "#8e8e93";

        avatarWrap.append(avatar, statusDot);

        const titleEl = createElement("h4", "fw-bold text-dark mb-1", name);
        titleEl.style.letterSpacing = "-0.4px";

        const badgeRow = createElement("div", "d-flex justify-content-center align-items-center gap-2 mt-2");

        const roleBadge = createElement("span", "badge rounded-pill px-3 py-1 fw-semibold", roleLabel);
        roleBadge.style.background = "rgba(0, 122, 255, 0.1)";
        roleBadge.style.color = "#007aff";
        roleBadge.style.fontSize = "0.75rem";

        const statusBadge = createElement("span", "badge rounded-pill px-3 py-1 fw-semibold", status);
        statusBadge.style.background = isActive ? "rgba(52, 199, 89, 0.12)" : "rgba(142, 142, 147, 0.14)";
        statusBadge.style.color = isActive ? "#248a3d" : "#636366";
        statusBadge.style.fontSize = "0.75rem";

        badgeRow.append(roleBadge, statusBadge);
        hero.append(avatarWrap, titleEl, badgeRow);

        // 2. iOS Grouped Inset List
        const groupBox = createElement("div", "rounded-4 overflow-hidden border");
        groupBox.style.background = "rgba(242, 242, 247, 0.65)";
        groupBox.style.borderColor = "rgba(60, 60, 67, 0.08)";

        appendIosRow(
            groupBox,
            "bi bi-envelope-fill text-primary",
            "rgba(0, 122, 255, 0.12)",
            "Email",
            user.email
        );
        appendIosRow(
            groupBox,
            "bi bi-shield-lock-fill text-warning",
            "rgba(255, 149, 0, 0.14)",
            "System Role",
            roleLabel
        );
        appendIosRow(
            groupBox,
            "bi bi-calendar-check-fill text-success",
            "rgba(52, 199, 89, 0.12)",
            "Joined Date",
            formatDate(user.createdAt)
        );
        appendIosRow(
            groupBox,
            "bi bi-fingerprint text-secondary",
            "rgba(142, 142, 147, 0.14)",
            "Account ID",
            String(user.id || "—"),
            true
        );

        // 3. Quick Edit Action Button (Uses your exact URLSearchParams logic)
        const actionWrap = createElement("div", "mt-4");
        const editBtn = createElement(
            "button",
            "btn w-100 py-2 fw-semibold d-flex align-items-center justify-content-center gap-2",
            ""
        );
        editBtn.type = "button";
        editBtn.style.background = "rgba(232, 40, 26, 0.1)";
        editBtn.style.color = "#e8281a";
        editBtn.style.borderRadius = "14px";
        editBtn.style.padding = "12px";

        editBtn.append(
            createElement("i", "bi bi-pencil-square"),
            document.createTextNode("Edit User Details")
        );

        editBtn.addEventListener("click", () => {
            navigateToEditUser(user);
        });

        actionWrap.appendChild(editBtn);

        elements.modalBody.append(hero, groupBox, actionWrap);

        bootstrap.Modal.getOrCreateInstance(elements.modal).show();
    }

    // Event Listeners
    elements.search.addEventListener("input", () => {
        clearTimeout(state.searchTimer);

        state.searchTimer = setTimeout(() => {
            state.searchTerm = elements.search.value.trim();
            state.pageNumber = 1;
            state.selectedIds.clear();
            loadUsers(1);
        }, 300);
    });

    elements.status.addEventListener("change", () => {
        state.status = elements.status.value;
        state.pageNumber = 1;
        state.selectedIds.clear();
        loadUsers(1);
    });

    elements.role.addEventListener("change", () => {
        state.role = elements.role.value;
        state.pageNumber = 1;
        state.selectedIds.clear();
        loadUsers(1);
    });

    elements.pageSize.addEventListener("change", () => {
        const requestedSize = Number(elements.pageSize.value);

        state.pageSize = [5, 10, 25, 50].includes(requestedSize)
            ? requestedSize
            : 10;

        state.pageNumber = 1;
        state.selectedIds.clear();
        loadUsers(1);
    });

    elements.selectAll.addEventListener("change", () => {
        for (const user of state.items) {
            const id = String(user.id);

            if (elements.selectAll.checked) {
                state.selectedIds.add(id);
            } else {
                state.selectedIds.delete(id);
            }
        }

        renderUsers();
        updateBulkbar();
    });

    elements.export.addEventListener("click",async () => {
        // 1. Sirf checked/selected users ko filter karein
        const selectedUsers = state.items
            .filter(user => state.selectedIds.has(String(user.id)))
            .map(user => ({
                FullName: String(user.fullName || "—"),
                Email: String(user.email || "—"),
                Role: normalizeRole(user.role),
                Status: getStatus(user),
                JoinedDate: formatDate(user.createdAt)
            }));
        if (selectedUsers.length === 0) {
            showError("Please select at least one user to export.");
            return;
        }

        // Button state change to Loading
        const originalText = elements.exportSelected.innerHTML;
        elements.exportSelected.disabled = true;
        elements.exportSelected.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span> Generating PDF...';

       try{
           // 2. Data Backend C# Controller ko bhejein
           const response = await fetch('/Admin/ExportUserPdf', {
               method: 'POST',
               headers: {
                   'Content-Type': 'application/json',
                   'Accept': 'application/pdf'
               },
               body: JSON.stringify(selectedUsers)
           });

           if (!response.ok) {
               throw new Error("Failed to generate PDF from server.");
           }
           // 3. Response se PDF blob le kar download trigger karein
           const blob = await response.blob();
           const downloadUrl = window.URL.createObjectURL(blob);

           const link = document.createElement('a');
           link.href = downloadUrl;
           link.download = "CanteenX_Selected_Users.pdf";
           document.body.appendChild(link);
           link.click();
           link.remove();

           // Cleanup
           setTimeout(() => window.URL.revokeObjectURL(downloadUrl), 1000);

        }


       catch (error) {
           Swal.fire('Export Failed', error.message, 'error');
       } finally {
           // Button state restore karein
           elements.exportSelected.innerHTML = originalText;
           elements.exportSelected.disabled = false;
       }

       
    });

    elements.exportSelected.addEventListener("click", () => {
        const selected = state.items.filter(user =>
            state.selectedIds.has(String(user.id))
        );

        exportRows(selected, "canteenx-selected-users.csv");
    });

    elements.retry.addEventListener("click", () => {
        loadUsers(state.pageNumber);
    });

    // Event delegation for View / Edit / Delete actions
    elements.tbody.addEventListener("click", event => {
        const button = event.target.closest("[data-action]");
        if (!button) return;

        const row = button.closest("tr");
        if (!row) return;

        const user = state.items.find(
            item => String(item.id) === row.dataset.id
        );

        if (!user) return;

        if (button.dataset.action === "view") {
            showProfile(user);
        } else if (button.dataset.action === "edit") {
            event.preventDefault();
            event.stopPropagation();
            navigateToEditUser(user);
        } else if (button.dataset.action === "delete") {
            const userId = String(user.id ?? "").trim();
            const userName = String(user.fullName || "this user");

            Swal.fire({
                title: 'Are you sure?',
                text: `You are about to delete ${userName}. This action cannot be undone!`,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#e8281a',
                cancelButtonColor: '#6c757d',
                confirmButtonText: 'Yes, delete it!',
                customClass: { popup: 'rounded-4 shadow-lg border' }
            }).then(async (result) => {
                if (result.isConfirmed) {
                    try {
                        // Ensure base API path matches your controller route
                        const response = await fetch(`/Admin/DeleteUser/${userId}`, {
                            method: "DELETE",
                            headers: {
                                "Accept": "application/json",
                                // "Authorization": `Bearer ${token}` // Agar JWT token chahiye toh yahan pass karein
                            }
                        });

                        const data = await response.json().catch(() => null);

                        if (!response.ok) {
                            throw new Error(data?.message || `Failed to delete user.`);
                        }

                        Swal.fire({
                            title: 'Deleted!',
                            text: 'User has been deleted successfully.',
                            icon: 'success',
                            confirmButtonColor: '#34c759',
                            timer: 2000,
                            customClass: { popup: 'rounded-4 shadow-lg border' }
                        });

                        loadUsers(state.pageNumber); // Table refresh
                    } catch (error) {
                        Swal.fire('Error!', error.message, 'error');
                    }

                }
            });
        }

            });

            // Initial request
            loadUsers(1);
        
})();