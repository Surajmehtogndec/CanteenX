

(() => {
    "use strict";
    

const endpoint = window.userDirectoryConfig?.endpoint;

    if (!endpoint) {
        showError(
            "User Directory endpoint is not configured. Check UserDirectory.cshtml."
        );
    
    } 

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
        pageSize: Number(elements.pageSize.value),
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
        return roleLabels[role] ? role : (role || "Unknown");
    }

    function getStatus(user) {
        // API DTO supplies IsActive; Pending is not inferred.
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
        elements.errorMessage.textContent = message;
        elements.error.classList.remove("d-none");
    }

    function hideError() {
        elements.error.classList.add("d-none");
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
      console.log("loadUsers() started");
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

            // Support both response shapes:
            // 1) API envelope: { success, message, data: { items, pageNumber, ... } }
            // 2) Direct MVC response: { items, pageNumber, ... }
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

            // Remove selections for records that are no longer on this page.
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

           
            // Last Active isn't provided by the current directory DTO.

            // Joined Date
            const joinedDate = formatDate(user.createdAt);

            const joinedDateCell = createElement(
                "td",
                "ud-lastactive",
                joinedDate
            );

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

        // Only current-page selections are exported by this view.
        elements.exportSelected.disabled = selectedVisible.length === 0;
    }

    function csvEscape(value) {
        let text = String(value ?? "");

        // Reduce CSV formula injection risks in spreadsheet applications.
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
            "Status",  "Last Active", "Joined Date"
        ];

        const lines = [
            header,
            ...users.map(user => [
                user.id,
                user.fullName,
                user.email,
                normalizeRole(user.role),
                getStatus(user),
               
                "", // Last Active is not supplied by the API DTO.
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

        // Delay revocation for compatibility with browser download handling.
        setTimeout(() => URL.revokeObjectURL(url), 1000);
    }

    function appendDetail(container, label, value) {
        const column = createElement("div", "col-12 col-sm-6");
        column.appendChild(createElement("small", "text-muted fw-bold text-uppercase", label));
        column.appendChild(createElement("div", "p-2 bg-light rounded-3 border mt-1", value || "—"));
        container.appendChild(column);
    }

    function showProfile(user) {
        elements.modalTitle.textContent = "User Profile";
        elements.modalBody.replaceChildren();

        const top = createElement("div", "d-flex align-items-center gap-3 mb-4");
        top.appendChild(
            createElement(
                "div",
                "ud-avatar",
                String(user.fullName || "?").trim().charAt(0).toUpperCase()
            )
        );

        const identity = createElement("div");
        identity.appendChild(
            createElement("h4", "mb-1 fw-bold text-dark", user.fullName || "Unnamed user")
        );
        identity.appendChild(
            createElement("div", "text-muted small", `Account ID: ${user.id}`)
        );

        top.appendChild(identity);

        const details = createElement("div", "row g-3");

        appendDetail(details, "Email", user.email);
        
        appendDetail(details, "System Role", normalizeRole(user.role));
        appendDetail(details, "Status", getStatus(user));
        appendDetail(details, "Joined", formatDate(user.createdAt));

        elements.modalBody.append(top, details);

        bootstrap.Modal.getOrCreateInstance(elements.modal).show();
    }

    // Search debounce prevents a request for every keystroke.
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

        // Keep client requests within the API's expected page-size limit.
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

    elements.export.addEventListener("click", () => {
        exportRows(state.items, "canteenx-users-current-page.csv");
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

    // Event delegation: works for rows created after each API response.
   
    // Event delegation: works for rows created after each API response.
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
        }
        else if (button.dataset.action === "edit") {
            event.preventDefault();
            event.stopPropagation();

            // Pehle userId declare karein.
            const userId = String(user.id ?? "").trim();

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
                id: String(user.id ?? ""),
                fullName: String(user.fullName ?? ""),
                email: String(user.email ?? ""),
                role: String(user.role ?? ""),
                isActive: String(Boolean(user.isActive))
            });
            window.location.href = `${editUrl}?${params.toString()}`;
        }
        else if (button.dataset.action === "delete") {
            showError(
                "Delete functionality has not been implemented yet."
            );
        }
    });

    // Initial request
    loadUsers(1);
})();
