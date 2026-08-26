(() => {
    let draggedCard = null;

    function getDropZone() {
        return document.querySelector(
            "[data-structured-component-drop-zone]");
    }

    function deactivateDropZone() {
        const dropZone = getDropZone();

        if (!dropZone) {
            return;
        }

        dropZone.classList.remove("active");
        dropZone.setAttribute("aria-hidden", "true");
    }

    document.addEventListener("dragstart", event => {
        const card = event.target.closest(
            "[data-structured-component-card]");

        if (!card) {
            return;
        }

        draggedCard = card;

        if (event.dataTransfer) {
            event.dataTransfer.effectAllowed = "copy";
            event.dataTransfer.setData(
                "text/plain",
                card.querySelector("strong")?.textContent ??
                    "Review component");
        }

        const dropZone = getDropZone();

        if (dropZone) {
            const componentName =
                card.querySelector("strong")?.textContent?.trim() ??
                "component";

            dropZone.querySelector("span").textContent =
                `Drop ${componentName} to create it in the review`;
            dropZone.classList.add("active");
            dropZone.setAttribute("aria-hidden", "false");
        }
    }, true);

    document.addEventListener("dragover", event => {
        const dropZone = event.target.closest(
            "[data-structured-component-drop-zone]");

        if (!draggedCard || !dropZone) {
            return;
        }

        event.preventDefault();

        if (event.dataTransfer) {
            event.dataTransfer.dropEffect = "copy";
        }
    }, true);

    document.addEventListener("drop", event => {
        const dropZone = event.target.closest(
            "[data-structured-component-drop-zone]");

        if (!draggedCard || !dropZone) {
            return;
        }

        event.preventDefault();

        const card = draggedCard;
        draggedCard = null;
        deactivateDropZone();
        card.click();
    }, true);

    document.addEventListener("dragend", () => {
        draggedCard = null;
        deactivateDropZone();
    }, true);
})();
