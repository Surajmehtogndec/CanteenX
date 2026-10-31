(() => {
    "use strict";

    const endpoint = window.canteenDirectoryConfig?.endpoint;

    // DOM Elements
    const elements = {
        search: document.getElementById("cdSearch"),
        status: document.getElementById("cdStatusFilter"),
        pageSize: document.getElementById("cdPageSize"),
        table: document.getElementById("cdTable"),
        tbody: document.getElementById("cdTableBody"),
        empty: document.getElementById("cdEmpty"),
        pagination: document.getElementById("cdPagination"),
        resultText: document.getElementById("cdResultText"),
        selectAll: document.getElementById("cdSelectAll"),
        errorMessage: document.getElementById("cdErrorMessage"),
        retryBtn: document.getElementById("cdRetry"),
        modal: document.getElementById("cdCanteenModal"),
        modalTitle: document.getElementById("cdModalTitle"),
        modalBody: document.getElementById("cdModalBody")
    };

    // State Management
    const state = {
        pageNumber: 1,
        pageSize: Number(elements.pageSize?.value) || 10,
        searchTerm: "",
        status: "",
        totalRecords: 0,
        totalPages: 0,
        items: [],
        selectedIds: new Set(),
        requestController: null,
        requestNumber: 0,
        searchTimer: null
    };

    // --- Helper Functions ---
    function formatDate(value) {
        if (!value) return "—";
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) return "—";

        return date.toLocaleDateString("en-US", {
            day: "2-digit",
            month: "short",
            year: "numeric"
        }); 
    }

    function createElement(tag, className, text) {
        const el = document.createElement(tag);
        if (className) el.className = className;
        if (text !== undefined) el.textContent = text;
        return el;
    }


    function showError(message) {
        if (elements.errorMessage) {
            elements.errorMessage.textContent = message;
            elements.errorMessage.parentElement.classList.remove("d-none");
        }
    }

    function hideError() {
        if (elements.errorMessage) {
            elements.errorMessage.parentElement.classList.add("d-none");
        }
    }

    function showLoading() {
        elements.tbody.replaceChildren();
        const row = createElement("tr");
        const cell = createElement("td", "text-center py-5");
        cell.colSpan = 6;
        cell.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status"></span>Loading Canteens...';
        row.appendChild(cell);
        elements.tbody.appendChild(row);
    }

    // --- API Interaction ---
    async function loadCanteens(pageNumber = state.pageNumber) {
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
            status: state.status
        });

        hideError();
        showLoading();
        elements.empty.classList.add("d-none");
        elements.table.classList.remove("d-none");

        try {
            const response = await fetch(`${endpoint}?${query.toString()}`, {
                method: "GET",
                headers: { "Accept": "application/json" },
                signal: controller.signal
            });

            if (requestNumber !== state.requestNumber) return;

            if (!response.ok) {
                throw new Error("Unable to retrieve canteens from server.");
            }

            const responseData = await response.json();
            
            if (responseData?.success === false) {
                throw new Error(responseData.message || "Failed to load canteens.");
            }

            const data = responseData?.data ?? responseData;

            state.items = data.items || [];
            state.pageNumber = Number(data.pageNumber) || state.pageNumber;
            state.pageSize = Number(data.pageSize) || state.pageSize;
            state.totalRecords = Number(data.totalRecords) || 0;
            state.totalPages = Number(data.totalPages) || 0;

            elements.pageSize.value = String(state.pageSize);

            // Maintain Selection state across pages
            const visibleIds = new Set(state.items.map(c => String(c.id)));
            for (const id of state.selectedIds) {
                if (!visibleIds.has(id)) {
                    state.selectedIds.delete(id);
                }
            }

            renderCanteens();
            renderPagination();
            renderSummary();

        } catch (error) {
            if (error.name === "AbortError") return;
            state.items = [];
            state.totalRecords = 0;
            state.totalPages = 0;

            elements.tbody.replaceChildren();
            elements.pagination.replaceChildren();
            elements.resultText.textContent = "Unable to load data.";
            showError(error.message || "An unexpected error occurred.");
        } finally {
            if (requestNumber === state.requestNumber) {
                state.requestController = null;
            }
        }
    }

    // --- Rendering Logic ---
    function renderCanteens() {
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

        for (const canteen of state.items) {
            // Using CanteenName as per new DTO
            const name = String(canteen.canteenName || "Unnamed Canteen"); 
            const location = String(canteen.location || "N/A");
            const id = String(canteen.CanteenId || "");

            const row = createElement("tr", "cd-row");
            row.dataset.id = id;
            row.dataset.name = name;

            // 1. Checkbox
            const checkCell = createElement("td");
            const checkbox = createElement("input", "cd-check cd-row-check");
            checkbox.type = "checkbox";
            checkbox.checked = state.selectedIds.has(id);
            checkbox.addEventListener("change", () => {
                if (checkbox.checked) state.selectedIds.add(id);
                else state.selectedIds.delete(id);
                updateSelectAll();
            });
            checkCell.appendChild(checkbox);

            // 2. Canteen Details (Image, Name, Location)
            const detailsCell = createElement("td");
            const infoContainer = createElement("div", "cd-info-container");
            const apiBaseUrl = "https://localhost:7150";

            if (canteen.imageUrl && canteen.imageUrl.trim() !== "") {
                const img = createElement("img", "cd-thumbnail");
                let imagePath = canteen.imageUrl.trim();

                if(imagePath.startsWith("http")){
                    img.src = imagePath;
                }
                else{
                    if(!imagePath.startsWith("/")){
                        imagePath = "/" + imagePath;
                    }
                  img.src = apiBaseUrl + imagePath;
                }
                
                img.alt = name;
                img.onerror = () => { img.outerHTML = '<div class="cd-thumbnail"><i class="bi bi-shop text-muted"></i></div>'; };
                infoContainer.appendChild(img);
            } else {
                const iconBox = createElement("div", "cd-thumbnail");
                iconBox.innerHTML = '<i class="bi bi-shop text-muted"></i>';
                infoContainer.appendChild(iconBox);
            }

            const textWrap = createElement("div");
            textWrap.appendChild(createElement("span", "cd-name", name));
            const locSpan = createElement("span", "cd-location");
            locSpan.innerHTML = `<i class="bi bi-geo-alt-fill text-danger me-1"></i> ${location}`;
            textWrap.appendChild(locSpan);
            
            infoContainer.appendChild(textWrap);
            detailsCell.appendChild(infoContainer);

            // 3. Description
            const descCell = createElement("td");
            const descDiv = createElement("div", "text-muted small");
            descDiv.style.maxWidth = "200px";
            descDiv.style.whiteSpace = "nowrap";
            descDiv.style.overflow = "hidden";
            descDiv.style.textOverflow = "ellipsis";
            descDiv.textContent = canteen.description || "—";
            descDiv.title = canteen.description || "";
            descCell.appendChild(descDiv);

            // 4. App Status
            const statusCell = createElement("td");
            const isActive = canteen.isActive === true;
            const statusBadge = createElement("span", `cd-badge ${isActive ? "cd-status-active" : "cd-status-inactive"}`);
            statusBadge.innerHTML = `<i class="bi ${isActive ? "bi-broadcast" : "bi-eye-slash-fill"}"></i> ${isActive ? "Active" : "Inactive"}`;
            statusCell.appendChild(statusBadge);

            // 5. Added On
            const dateCell = createElement("td", "text-muted fw-medium small", formatDate(canteen.createdAt));

            // 6. Actions Dropdown
            const actionCell = createElement("td", "text-end");
            actionCell.innerHTML = `
                <div class="dropdown">
                    <button class="cd-action-btn" type="button" data-bs-toggle="dropdown"><i class="bi bi-three-dots"></i></button>
                    <ul class="dropdown-menu dropdown-menu-end cd-dropdown">
                        <li><button class="dropdown-item" type="button" data-action="menu"><i class="bi bi-egg-fried text-primary"></i> View </button></li>
                        <li><hr class="dropdown-divider" /></li>
                        <li><button class="dropdown-item delete-item" type="button" data-action="delete"><i class="bi bi-trash3-fill"></i> Delete Canteen</button></li>
                    </ul>
                </div>
            `;

            row.append(checkCell, detailsCell, descCell, statusCell, dateCell, actionCell);
            fragment.appendChild(row);
        }

        elements.tbody.appendChild(fragment);
        updateSelectAll();
    }

    function renderSummary() {
        if (state.totalRecords === 0) {
            elements.resultText.textContent = "Showing 0 canteens";
            return;
        }

        const start = ((state.pageNumber - 1) * state.pageSize) + 1;
        const end = Math.min(state.pageNumber * state.pageSize, state.totalRecords);
        elements.resultText.textContent = `Showing ${start}–${end} of ${state.totalRecords} canteens`;
    }

    function renderPagination() {
        elements.pagination.replaceChildren();
        if (state.totalPages <= 1) return;

        const createPageBtn = (label, pageNum, disabled, isActive = false) => {
            const btn = createElement("button", `cd-page-btn btn btn-sm ${isActive ? "btn-primary active" : "btn-outline-secondary"} mx-1`, label);
            btn.disabled = disabled;
            btn.addEventListener("click", () => loadCanteens(pageNum));
            return btn;
        };

        // Previous
        elements.pagination.appendChild(createPageBtn("Prev", state.pageNumber - 1, state.pageNumber <= 1));

        // Pages (Simplified logic for brevity)
        for (let i = 1; i <= state.totalPages; i++) {
            if (i === 1 || i === state.totalPages || (i >= state.pageNumber - 1 && i <= state.pageNumber + 1)) {
                elements.pagination.appendChild(createPageBtn(String(i), i, false, i === state.pageNumber));
            } else if (i === state.pageNumber - 2 || i === state.pageNumber + 2) {
                const dots = createElement("span", "mx-1 text-muted", "...");
                elements.pagination.appendChild(dots);
            }
        }

        // Next
        elements.pagination.appendChild(createPageBtn("Next", state.pageNumber + 1, state.pageNumber >= state.totalPages));
    }

    function updateSelectAll() {
        const checkboxes = [...elements.tbody.querySelectorAll(".cd-row-check")];
        elements.selectAll.checked = checkboxes.length > 0 && checkboxes.every(cb => cb.checked);
        elements.selectAll.indeterminate = checkboxes.some(cb => cb.checked) && !checkboxes.every(cb => cb.checked);
    }
    // =========================================================
    // PREMIUM iOS CANTEEN PROFILE MODAL
    // =========================================================
    function appendIosRow(list, iconClass, iconBg, label, value, isLast = false) {
        const item = createElement("div", `d-flex align-items-center justify-content-between py-3 px-3 ${isLast ? "" : "border-bottom"}`);
        item.style.borderColor = "rgba(60, 60, 67, 0.08)";

        const left = createElement("div", "d-flex align-items-center gap-3");
        const iconBox = createElement("div", "d-flex align-items-center justify-content-center rounded-3");
        iconBox.style.width = "34px"; iconBox.style.height = "34px"; iconBox.style.background = iconBg;
        iconBox.appendChild(createElement("i", `${iconClass} fs-6`));

        left.append(iconBox, createElement("span", "fw-medium text-secondary small", label));
        
        const right = createElement("span", "fw-semibold text-dark small text-end text-break ms-3", value || "—");
        
        item.append(left, right);
        list.appendChild(item);
    }

    function showCanteenProfile(canteen) {
        elements.modalTitle.textContent = "Canteen Details";
        elements.modalBody.replaceChildren();

        const name = String(canteen.canteenName || "Unnamed Canteen");
        const location = String(canteen.location || "N/A");
        const isActive = canteen.isActive === true;
        const status = isActive ? "Active" : "Inactive";
        const apiBaseUrl = "https://localhost:7150"; // Update with your actual API Port if changed

        // 1. iOS Hero Header
        const hero = createElement("div", "text-center pb-4 pt-2");
        const avatarWrap = createElement("div", "position-relative d-inline-block mb-3");
        
        let avatar;
        if (canteen.imageUrl && canteen.imageUrl.trim() !== "") {
            avatar = createElement("img", "shadow");
            let imagePath = canteen.imageUrl.trim();
            if (!imagePath.startsWith("http")) {
                if (!imagePath.startsWith("/")) imagePath = "/" + imagePath;
                imagePath = apiBaseUrl + imagePath;
            }
            avatar.src = imagePath;
            avatar.style.objectFit = "cover";
            avatar.onerror = () => { avatar.src = ""; avatar.style.background = "#e2e8f0"; };
        } else {
            avatar = createElement("div", "d-flex align-items-center justify-content-center fw-bold text-white shadow");
            avatar.innerHTML = '<i class="bi bi-shop"></i>';
            avatar.style.background = "linear-gradient(135deg, #007aff 0%, #005bb5 100%)";
        }

        avatar.style.width = "80px"; avatar.style.height = "80px";
        avatar.style.fontSize = "2rem"; avatar.style.borderRadius = "24px";
        avatar.style.boxShadow = "0 12px 24px rgba(0, 122, 255, 0.25)";

        const statusDot = createElement("span", "position-absolute");
        statusDot.style.width = "16px"; statusDot.style.height = "16px";
        statusDot.style.borderRadius = "50%"; statusDot.style.bottom = "2px"; statusDot.style.right = "2px";
        statusDot.style.border = "2.5px solid #ffffff";
        statusDot.style.backgroundColor = isActive ? "#34c759" : "#8e8e93";

        avatarWrap.append(avatar, statusDot);
        const titleEl = createElement("h4", "fw-bold text-dark mb-1", name);
        titleEl.style.letterSpacing = "-0.4px";

        const statusBadge = createElement("span", "badge rounded-pill px-3 py-1 fw-semibold mt-2", status);
        statusBadge.style.background = isActive ? "rgba(52, 199, 89, 0.12)" : "rgba(142, 142, 147, 0.14)";
        statusBadge.style.color = isActive ? "#248a3d" : "#636366";
        statusBadge.style.fontSize = "0.75rem";

        hero.append(avatarWrap, titleEl, statusBadge);

        // 2. iOS Grouped Inset List
        const groupBox = createElement("div", "rounded-4 overflow-hidden border");
        groupBox.style.background = "rgba(242, 242, 247, 0.65)";
        groupBox.style.borderColor = "rgba(60, 60, 67, 0.08)";

        appendIosRow(groupBox, "bi bi-geo-alt-fill text-danger", "rgba(255, 59, 48, 0.12)", "Location", location);
        appendIosRow(groupBox, "bi bi-card-text text-primary", "rgba(0, 122, 255, 0.12)", "Description", canteen.description);
        appendIosRow(groupBox, "bi bi-calendar-check-fill text-success", "rgba(52, 199, 89, 0.12)", "Added On", formatDate(canteen.createdAt));
     

        elements.modalBody.append(hero, groupBox, actionWrap);
        bootstrap.Modal.getOrCreateInstance(elements.modal).show();
    }





    // --- Event Listeners ---
    elements.search?.addEventListener("input", () => {
        clearTimeout(state.searchTimer);
        state.searchTimer = setTimeout(() => {
            state.searchTerm = elements.search.value.trim();
            state.selectedIds.clear();
            loadCanteens(1);
        }, 400);
    });

    elements.status?.addEventListener("change", () => {
        state.status = elements.status.value;
        state.selectedIds.clear();
        loadCanteens(1);
    });

    elements.pageSize?.addEventListener("change", () => {
        state.pageSize = Number(elements.pageSize.value) || 10;
        state.selectedIds.clear();
        loadCanteens(1);
    });

    elements.selectAll?.addEventListener("change", () => {
        for (const canteen of state.items) {
            const id = String(canteen.CanteenId);
            if (elements.selectAll.checked) state.selectedIds.add(id);
            else state.selectedIds.delete(id);
        }
        renderCanteens();
    });

    if (elements.retryBtn) {
        elements.retryBtn.addEventListener("click", () => loadCanteens(state.pageNumber));
    }
    // Action Delegation (View, Edit, Delete)
    elements.tbody.addEventListener("click", event => {
        const button = event.target.closest("buttpn[data-action]");
        if (!button) return;

        const row = button.closest("tr");
        if (!row) return;

        // Table ki ID se us poore Canteen data ko array mein dhundein
        const canteen = state.items.find(item => String(item.canteenId) === row.dataset.id);
        if (!canteen) return;

        const name = String(canteen.canteenName || "Unnamed Canteen");

        if (button.dataset.action === "view") {
            console.log("view button clicked");
            showCanteenProfile(canteen); 
        } else if (button.dataset.action === "edit") {
            window.location.href = `${config.editUrl}?id=${canteen.id}`;
        } else if (button.dataset.action === "delete") {
            Swal.fire({
                titleText: 'Delete Canteen?',
                text: `Are you sure you want to permanently remove ${name}? All associated menus will be deleted.`,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#ff3b30',
                cancelButtonColor: '#8e8e93',
                confirmButtonText: 'Yes, delete it!',
                background: 'rgba(255, 255, 255, 0.95)',
                customClass: { popup: 'rounded-4 shadow-lg border' }
            }).then(async (result) => {
                if (result.isConfirmed) {
                    try {
                        const response = await fetch(`/Admin/DeleteCanteen/${canteen.id}`, { method: "DELETE" });
                        if (!response.ok) throw new Error("Failed to delete canteen.");
                        
                        Swal.fire({ toast: true, position: 'top-end', icon: 'success', titleText: 'Canteen Deleted.', showConfirmButton: false, timer: 3000 });
                        loadCanteens(state.pageNumber);
                    } catch (err) {
                        Swal.fire('Error', err.message, 'error');
                    }
                }
            });
        }
    });
    // Initial Load
    loadCanteens(1);
})();