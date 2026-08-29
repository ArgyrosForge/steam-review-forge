(() => {
    const storageKey = "steam-review-forge-appearance-v1";
    const legacyStorageKey = "steam-review-forge-theme";

    const themes = Object.freeze([
        Object.freeze({
            id: "main-blue",
            name: "Main Blue"
        }),
        Object.freeze({
            id: "nord",
            name: "Nord"
        })
    ]);

    const themeIds = new Set(themes.map(theme => theme.id));
    const colorModes = new Set(["dark", "light"]);

    const defaultAppearance = Object.freeze({
        theme: "main-blue",
        colorMode: "dark"
    });

    function normalizeAppearance(value) {
        return {
            theme: themeIds.has(value?.theme)
                ? value.theme
                : defaultAppearance.theme,

            colorMode: colorModes.has(value?.colorMode)
                ? value.colorMode
                : defaultAppearance.colorMode
        };
    }

    function getStoredAppearance() {
        try {
            const storedAppearance = localStorage.getItem(storageKey);

            if (storedAppearance) {
                return normalizeAppearance(
                    JSON.parse(storedAppearance));
            }

            const legacyColorMode =
                localStorage.getItem(legacyStorageKey);

            if (colorModes.has(legacyColorMode)) {
                return normalizeAppearance({
                    theme: defaultAppearance.theme,
                    colorMode: legacyColorMode
                });
            }
        } catch {
            // Continue with defaults when storage is unavailable or invalid.
        }

        return { ...defaultAppearance };
    }

    function applyAppearance(value, persist = false) {
        const appearance = normalizeAppearance(value);

        document.documentElement.dataset.theme = appearance.theme;
        document.documentElement.dataset.colorMode =
            appearance.colorMode;

        if (persist) {
            try {
                localStorage.setItem(
                    storageKey,
                    JSON.stringify(appearance));

                localStorage.removeItem(legacyStorageKey);
            } catch {
                // Continue without persistence if storage is unavailable.
            }
        }

        return appearance;
    }

    let appearance = applyAppearance(getStoredAppearance());

    window.themeManager = {
        get: () => ({ ...appearance }),

        getThemes: () =>
            themes.map(theme => ({ ...theme })),

        setTheme: theme => {
            appearance = applyAppearance(
                {
                    ...appearance,
                    theme
                },
                true);

            return { ...appearance };
        },

        setColorMode: colorMode => {
            appearance = applyAppearance(
                {
                    ...appearance,
                    colorMode
                },
                true);

            return { ...appearance };
        },

        toggleColorMode: () => {
            const colorMode =
                appearance.colorMode === "dark"
                    ? "light"
                    : "dark";

            appearance = applyAppearance(
                {
                    ...appearance,
                    colorMode
                },
                true);

            return { ...appearance };
        }
    };
})();
