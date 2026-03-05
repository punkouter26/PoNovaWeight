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
     * Retrieves dev token from local storage
     */
    getDevToken: function() {
        try {
            return localStorage.getItem('dev_auth_token');
        } catch (error) {
            console.error('Error retrieving dev token:', error);
            return null;
        }
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
    }
};
