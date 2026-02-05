/**
 * Image utilities for PoNovaWeight meal scanning.
 */

/**
 * Reads a file input element and returns the base64 encoded data.
 * @param {HTMLInputElement} inputElement - The file input element
 * @returns {Promise<{base64: string, width: number, height: number}>}
 */
window.readFileAsBase64 = async function (inputElement) {
    const file = inputElement.files?.[0];
    if (!file) {
        return null;
    }

    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        
        reader.onload = async function (e) {
            const dataUrl = e.target.result;
            // Extract base64 portion (after the comma in data:image/xxx;base64,...)
            const base64 = dataUrl.split(',')[1];
            
            // Get image dimensions
            const img = new Image();
            img.onload = function () {
                resolve({
                    base64: base64,
                    width: img.width,
                    height: img.height
                });
            };
            img.onerror = function () {
                // Return base64 even if we can't get dimensions
                resolve({
                    base64: base64,
                    width: 0,
                    height: 0
                });
            };
            img.src = dataUrl;
        };
        
        reader.onerror = function () {
            reject(new Error('Failed to read file'));
        };
        
        reader.readAsDataURL(file);
    });
};

/**
 * Compresses an image to reduce file size for API transmission.
 * @param {string} base64 - Base64 encoded image data
 * @param {number} maxDimension - Maximum width or height (default 1200)
 * @param {number} quality - JPEG quality 0-1 (default 0.8)
 * @returns {Promise<{base64: string, width: number, height: number}>}
 */
window.compressImage = function (base64, maxDimension = 1200, quality = 0.8) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        
        img.onload = function () {
            // Calculate new dimensions maintaining aspect ratio
            let { width, height } = img;
            
            if (width > maxDimension || height > maxDimension) {
                if (width > height) {
                    height = Math.round(height * (maxDimension / width));
                    width = maxDimension;
                } else {
                    width = Math.round(width * (maxDimension / height));
                    height = maxDimension;
                }
            }
            
            // Create canvas and draw resized image
            const canvas = document.createElement('canvas');
            canvas.width = width;
            canvas.height = height;
            
            const ctx = canvas.getContext('2d');
            ctx.drawImage(img, 0, 0, width, height);
            
            // Convert to JPEG for consistent format and compression
            const compressedDataUrl = canvas.toDataURL('image/jpeg', quality);
            const compressedBase64 = compressedDataUrl.split(',')[1];
            
            resolve({
                base64: compressedBase64,
                width: width,
                height: height
            });
        };
        
        img.onerror = function () {
            // Return original if compression fails
            resolve({
                base64: base64,
                width: 0,
                height: 0
            });
        };
        
        img.src = `data:image/jpeg;base64,${base64}`;
    });
};
