(() => {
    let draggedCard = null;
    let activeTarget = null;
    let dotNetReference = null;

    function getComponentList() {
        return document.querySelector(
            "[data-structured-component-list]");
    }

    function getInsertionTargets() {
        return document.querySelectorAll(
            "[data-structured-component-insert-target]");
    }

    function deactivateInsertionTargets() {
        const componentList = getComponentList();

        componentList?.classList.remove("palette-drag-active");

        for (const target of getInsertionTargets()) {
            target.classList.remove("drag-over");
            target.setAttribute("aria-hidden", "true");
        }

        activeTarget = null;
    }

    function activateInsertionTargets(componentName) {
        const componentList = getComponentList();

        if (!componentList) {
            return;
        }

        componentList.classList.add("palette-drag-active");

        for (const target of getInsertionTargets()) {
            const label = target.querySelector("span");

            if (label) {
                label.textContent = `Insert ${componentName} here`;
            }

            target.setAttribute("aria-hidden", "false");
        }
    }

    document.addEventListener("dragstart", event => {
        const card = event.target.closest(
            "[data-structured-component-card]");

        if (!card) {
            return;
        }

        draggedCard = card;

        const componentName =
            card.querySelector("strong")?.textContent?.trim() ??
            "component";

        if (event.dataTransfer) {
            event.dataTransfer.effectAllowed = "copy";
            event.dataTransfer.setData(
                "text/plain",
                card.dataset.structuredComponentKey ??
                    componentName);
        }

        activateInsertionTargets(componentName);
    }, true);

    document.addEventListener("dragover", event => {
        const target = event.target.closest(
            "[data-structured-component-insert-target]");

        if (!draggedCard || !target) {
            return;
        }

        event.preventDefault();

        if (activeTarget !== target) {
            activeTarget?.classList.remove("drag-over");
            activeTarget = target;
            activeTarget.classList.add("drag-over");
        }

        if (event.dataTransfer) {
            event.dataTransfer.dropEffect = "copy";
        }
    }, true);

    document.addEventListener("dragleave", event => {
        const target = event.target.closest(
            "[data-structured-component-insert-target]");

        if (!target || target !== activeTarget) {
            return;
        }

        if (event.relatedTarget &&
            target.contains(event.relatedTarget)) {
            return;
        }

        target.classList.remove("drag-over");
        activeTarget = null;
    }, true);

    document.addEventListener("drop", async event => {
        const target = event.target.closest(
            "[data-structured-component-insert-target]");

        if (!draggedCard || !target || !dotNetReference) {
            return;
        }

        event.preventDefault();

        const templateKey =
            draggedCard.dataset.structuredComponentKey;
        const insertionIndex = Number.parseInt(
            target.dataset.insertionIndex ?? "",
            10);

        draggedCard = null;
        deactivateInsertionTargets();

        if (!templateKey || !Number.isInteger(insertionIndex)) {
            return;
        }

        await dotNetReference.invokeMethodAsync(
            "AddStructuredComponentAtAsync",
            templateKey,
            insertionIndex);
    }, true);

    document.addEventListener("dragend", () => {
        draggedCard = null;
        deactivateInsertionTargets();
    }, true);

    window.structuredComponentDrag = {
        initialize(reference) {
            dotNetReference = reference;
            deactivateInsertionTargets();
        }
    };
})();
