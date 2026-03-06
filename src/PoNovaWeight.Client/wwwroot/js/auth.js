// Authentication token utilities for Blazor Client
window.PoNovaWeightAuth = {
    /**
     * Retrieves OIDC token from session storage
     * Iterates through session storage keys looking for OIDC state with id_token
     */
    getOidcToken: function() {
        try {
            for (let i = 0; i < sessionStorage.length; i++) {
                const key = sessionStorage.key(i);
                if (key && key.startsWith('oidc.')) {
                    const raw = sessionStorage.getItem(key);
                    if (raw) {
                        const val = JSON.parse(raw);
                        if (val && val.id_token) {
                            return val.id_token;
                        }
                    }
                }
            }
        } catch (error) {
            console.error('Error retrieving OIDC token:', error);
        }
        return null;
    },

    /**
     * Retrieves Microsoft token from session storage
     */
    getMicrosoftToken: function() {
        try {
            return sessionStorage.getItem('microsoft_id_token');
        } catch (error) {
            console.error('Error retrieving Microsoft token:', error);
            return null;
        }
    },

    /**
     * Extracts id_token from URL hash fragment (OAuth implicit flow)
     * @returns {string|null} The id_token value or null if not found
     */
    extractTokenFromUrlHash: function() {
        try {
            const hash = window.location.hash.substring(1);
            if (!hash) return null;
            
            const params = new URLSearchParams(hash);
            return params.get('id_token');
        } catch (error) {
            console.error('Error extracting token from URL hash:', error);
            return null;
        }
    }
};
