window.validationNavigator = {
    focus: function (field) {
        const target = Array.from(
            document.querySelectorAll("[data-validation-field]"))
            .find(element =>
                element.getAttribute("data-validation-field") === field);

        if (!target) {
            return;
        }

        const focusTarget = target.matches(
            "button, input, select, textarea, [tabindex]")
            ? target
            : target.querySelector(
                "button, input, select, textarea, [tabindex]");

        target.scrollIntoView({
            behavior: window.matchMedia("(prefers-reduced-motion: reduce)").matches
                ? "auto"
                : "smooth",
            block: "center",
            inline: "nearest"
        });

        if (focusTarget) {
            focusTarget.focus({ preventScroll: true });
        }

        target.classList.remove("validation-focus-target");
        void target.offsetWidth;
        target.classList.add("validation-focus-target");

        window.setTimeout(
            () => target.classList.remove("validation-focus-target"),
            1800);
    }
};
