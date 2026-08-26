const bbCodeEditorHistories = new WeakMap();

function getBbCodeEditorHistory(editor) {
    let history = bbCodeEditorHistories.get(editor);

    if (history) {
        return history;
    }

    history = {
        undoStack: []
    };

    editor.addEventListener("beforeinput", function (event) {
        if (event.inputType === "historyUndo" ||
            event.inputType === "historyRedo") {
            return;
        }

        rememberBbCodeEditorState(editor, history);
    });

    bbCodeEditorHistories.set(editor, history);
    return history;
}

function rememberBbCodeEditorState(editor, history) {
    const snapshot = {
        value: editor.value,
        selectionStart: editor.selectionStart ?? editor.value.length,
        selectionEnd: editor.selectionEnd ?? editor.value.length
    };
    const latest = history.undoStack.at(-1);

    if (latest &&
        latest.value === snapshot.value &&
        latest.selectionStart === snapshot.selectionStart &&
        latest.selectionEnd === snapshot.selectionEnd) {
        return;
    }

    history.undoStack.push(snapshot);

    if (history.undoStack.length > 100) {
        history.undoStack.shift();
    }
}

window.bbCodeEditor = {
    initialize: function (editorId) {
        const editor = document.getElementById(editorId);

        if (editor) {
            getBbCodeEditorHistory(editor);
        }
    },

    insertTemplate: function (editorId, template) {
        const editor = document.getElementById(editorId);

        if (!editor) {
            return;
        }

        const history = getBbCodeEditorHistory(editor);
        rememberBbCodeEditorState(editor, history);

        const start = editor.selectionStart ?? editor.value.length;
        const end = editor.selectionEnd ?? start;
        const before = editor.value.slice(0, start);
        const after = editor.value.slice(end);
        const prefix = before.length > 0 && !before.endsWith("\n")
            ? "\n\n"
            : "";
        const suffix = after.length > 0 && !after.startsWith("\n")
            ? "\n\n"
            : "";
        const insertion = prefix + template + suffix;

        editor.setRangeText(insertion, start, end, "end");
        editor.dispatchEvent(new Event("input", { bubbles: true }));
        editor.focus();
    },

    undo: function (editorId) {
        const editor = document.getElementById(editorId);

        if (!editor) {
            return;
        }

        const history = getBbCodeEditorHistory(editor);
        const snapshot = history.undoStack.pop();

        if (!snapshot) {
            editor.focus();
            return;
        }

        editor.value = snapshot.value;
        editor.setSelectionRange(
            snapshot.selectionStart,
            snapshot.selectionEnd);
        editor.dispatchEvent(new Event("input", { bubbles: true }));
        editor.focus();
    },

    clear: function (editorId) {
        const editor = document.getElementById(editorId);

        if (!editor || editor.value.length === 0) {
            editor?.focus();
            return;
        }

        const history = getBbCodeEditorHistory(editor);
        rememberBbCodeEditorState(editor, history);

        editor.value = "";
        editor.setSelectionRange(0, 0);
        editor.dispatchEvent(new Event("input", { bubbles: true }));
        editor.focus();
    }
};
