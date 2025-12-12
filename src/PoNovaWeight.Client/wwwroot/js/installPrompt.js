// PWA Install Prompt Handler
// Manages the beforeinstallprompt event and provides install functionality

let deferredPrompt = null;
let dotNetHelper = null;
let isPromptDismissed = false;

/**
 * Initialize the install prompt handler
 * @param {DotNetObjectReference} helper - Reference to Blazor component
 */
export function initialize(helper) {
    dotNetHelper = helper;
    
    // Check if app is already installed
    if (window.matchMedia('(display-mode: standalone)').matches) {
        console.log('[Install Prompt] App already installed');
        return;
    }
    
    // Check if user previously dismissed
    if (localStorage.getItem('nova_install_dismissed')) {
        const dismissedAt = parseInt(localStorage.getItem('nova_install_dismissed'), 10);
        const daysSinceDismissed = (Date.now() - dismissedAt) / (1000 * 60 * 60 * 24);
        
        // Only show again after 7 days
        if (daysSinceDismissed < 7) {
            console.log('[Install Prompt] User dismissed recently');
            isPromptDismissed = true;
            return;
        }
    }
    
    // Listen for the beforeinstallprompt event
    window.addEventListener('beforeinstallprompt', handleBeforeInstallPrompt);
    
    // Track when app is installed
    window.addEventListener('appinstalled', handleAppInstalled);
}

/**
 * Handle the beforeinstallprompt event
 * @param {Event} e - The beforeinstallprompt event
 */
function handleBeforeInstallPrompt(e) {
    console.log('[Install Prompt] beforeinstallprompt fired');
    
    // Prevent the mini-infobar from appearing
    e.preventDefault();
    
    // Store the event for later use
    deferredPrompt = e;
    
    // Only show if not previously dismissed
    if (!isPromptDismissed && dotNetHelper) {
        // Delay showing the prompt by 30 seconds for better UX
        setTimeout(() => {
            if (deferredPrompt && dotNetHelper) {
                dotNetHelper.invokeMethodAsync('ShowInstallPrompt');
            }
        }, 30000);
    }
}

/**
 * Handle app installed event
 */
function handleAppInstalled() {
    console.log('[Install Prompt] App was installed');
    deferredPrompt = null;
    
    // Clear the dismissed flag
    localStorage.removeItem('nova_install_dismissed');
}

/**
 * Trigger the install prompt
 * @returns {Promise<boolean>} Whether the user accepted the install
 */
export async function promptInstall() {
    if (!deferredPrompt) {
        console.log('[Install Prompt] No deferred prompt available');
        return false;
    }
    
    try {
        // Show the install prompt
        deferredPrompt.prompt();
        
        // Wait for the user response
        const { outcome } = await deferredPrompt.userChoice;
        
        console.log('[Install Prompt] User choice:', outcome);
        
        // Clear the deferred prompt
        deferredPrompt = null;
        
        return outcome === 'accepted';
    } catch (error) {
        console.error('[Install Prompt] Error showing prompt:', error);
        return false;
    }
}

/**
 * Dismiss the install prompt and remember the choice
 */
export function dismissPrompt() {
    console.log('[Install Prompt] User dismissed prompt');
    localStorage.setItem('nova_install_dismissed', Date.now().toString());
    isPromptDismissed = true;
}

/**
 * Check if the app is installed
 * @returns {boolean} Whether the app is installed
 */
export function isInstalled() {
    return window.matchMedia('(display-mode: standalone)').matches ||
           window.navigator.standalone === true;
}

/**
 * Check if install is available
 * @returns {boolean} Whether install prompt is available
 */
export function isInstallAvailable() {
    return deferredPrompt !== null;
}
