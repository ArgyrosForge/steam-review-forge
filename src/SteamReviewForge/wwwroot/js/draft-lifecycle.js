let hasUnsavedChanges = false;

function handleBeforeUnload(event) {
    if (!hasUnsavedChanges) {
        return;
    }

    event.preventDefault();
    event.returnValue = "";
}

window.reviewDraftLifecycle = {
    setUnsavedChanges: function (value) {
        const nextValue = Boolean(value);

        if (nextValue === hasUnsavedChanges) {
            return;
        }

        hasUnsavedChanges = nextValue;

        if (hasUnsavedChanges) {
            window.addEventListener("beforeunload", handleBeforeUnload);
        } else {
            window.removeEventListener("beforeunload", handleBeforeUnload);
        }
    },

    hasUnsavedChanges: function () {
        return hasUnsavedChanges;
    }
};
