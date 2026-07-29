(() => {
    let dragPreview;

    const removeDragPreview = () => {
        dragPreview?.remove();
        dragPreview = undefined;
        document.documentElement.classList.remove("table-drag-active");
    };

    const copyFormValues = (source, clone) => {
        const sourceControls = source.querySelectorAll("input, select, textarea");
        const cloneControls = clone.querySelectorAll("input, select, textarea");

        sourceControls.forEach((control, index) => {
            const cloneControl = cloneControls[index];

            if (!cloneControl) {
                return;
            }

            if ("value" in cloneControl) {
                cloneControl.value = control.value;
            }

            if ("checked" in cloneControl) {
                cloneControl.checked = control.checked;
            }
        });
    };

    document.addEventListener("dragstart", event => {
        if (event.target.closest("input, button, select, textarea")) {
            event.preventDefault();
            event.stopImmediatePropagation();
            return;
        }

        const handle = event.target.closest("[data-table-drag-handle]");

        if (!handle || !event.dataTransfer) {
            return;
        }

        const source = handle.closest("[data-drag-preview]");

        if (!source) {
            return;
        }

        removeDragPreview();

        const bounds = source.getBoundingClientRect();
        const clone = source.cloneNode(true);
        copyFormValues(source, clone);

        dragPreview = document.createElement("div");
        dragPreview.className = "table-drag-preview";
        dragPreview.style.width = `${Math.min(bounds.width, 720)}px`;

        if (source.tagName === "TR" || source.tagName === "TH") {
            const table = document.createElement("table");
            table.className = "preview-table table-editor";

            if (source.tagName === "TR") {
                const body = document.createElement("tbody");
                body.append(clone);
                table.append(body);
            } else {
                const head = document.createElement("thead");
                const row = document.createElement("tr");
                row.append(clone);
                head.append(row);
                table.append(head);
            }

            dragPreview.append(table);
        } else {
            dragPreview.append(clone);
        }

        document.body.append(dragPreview);

        document.documentElement.classList.add("table-drag-active");
        event.dataTransfer.effectAllowed = "move";
        event.dataTransfer.setDragImage(
            dragPreview,
            Math.min(28, bounds.width / 2),
            Math.min(20, bounds.height / 2));
    });

    document.addEventListener("dragend", removeDragPreview);
    document.addEventListener("drop", removeDragPreview);
})();
