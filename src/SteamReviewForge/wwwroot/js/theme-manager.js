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
        }),
        Object.freeze({
            id: "catppuccin-latte",
            name: "Catppuccin Latte",
            colorMode: "light"
        }),
        Object.freeze({
            id: "catppuccin-frappe",
            name: "Catppuccin Frappé",
            colorMode: "dark"
        }),
        Object.freeze({
            id: "catppuccin-macchiato",
            name: "Catppuccin Macchiato",
            colorMode: "dark"
        }),
        Object.freeze({
            id: "catppuccin-mocha",
            name: "Catppuccin Mocha",
            colorMode: "dark"
        })
    ]);

    const themeIds = new Set(themes.map(theme => theme.id));
    const themesById = new Map(
        themes.map(theme => [theme.id, theme]));
    const colorModes = new Set(["dark", "light"]);

    const defaultAppearance = Object.freeze({
        theme: "main-blue",
        colorMode: "dark"
    });

    function normalizeAppearance(value) {
        const theme = themeIds.has(value?.theme)
            ? value.theme
            : defaultAppearance.theme;

        const nativeColorMode = themesById.get(theme)?.colorMode;

        return {
            theme,

            colorMode: nativeColorMode ??
                (colorModes.has(value?.colorMode)
                    ? value.colorMode
                    : defaultAppearance.colorMode)
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
    let lastCatppuccinDarkTheme =
        appearance.theme.startsWith("catppuccin-") &&
        appearance.colorMode === "dark"
            ? appearance.theme
            : "catppuccin-mocha";

    window.themeManager = {
        get: () => ({ ...appearance }),

        getThemes: () =>
            themes.map(theme => ({
                id: theme.id,
                name: theme.name
            })),

        setTheme: theme => {
            appearance = applyAppearance(
                {
                    ...appearance,
                    theme
                },
                true);

            if (appearance.theme.startsWith("catppuccin-") &&
                appearance.colorMode === "dark") {
                lastCatppuccinDarkTheme = appearance.theme;
            }

            return { ...appearance };
        },

        setColorMode: colorMode => {
            let theme = appearance.theme;

            if (theme.startsWith("catppuccin-")) {
                if (colorMode === "light") {
                    if (appearance.colorMode === "dark") {
                        lastCatppuccinDarkTheme = theme;
                    }

                    theme = "catppuccin-latte";
                } else if (colorMode === "dark") {
                    theme = lastCatppuccinDarkTheme;
                }
            }

            appearance = applyAppearance(
                {
                    ...appearance,
                    theme,
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

            return window.themeManager.setColorMode(colorMode);
        }
    };
})();
