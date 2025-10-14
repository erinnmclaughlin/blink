// Direct upload to Azure Blob Storage with progress tracking
// This uses the BlockBlobClient from @azure/storage-blob (loaded via CDN)

class DirectUploadManager {
    constructor() {
        this.blockBlobClient = null;
        this.abortController = null;
    }

    /**
     * Wait for Azure SDK to be loaded
     */
    async waitForAzureSDK(maxWaitMs = 5000) {
        const startTime = Date.now();
        while (!window.Azure?.Storage?.Blob) {
            if (Date.now() - startTime > maxWaitMs) {
                console.error('Azure SDK check failed. Available globals:', Object.keys(window).filter(k => k.toLowerCase().includes('azure')));
                console.error('window.Azure:', window.Azure);
                throw new Error('Azure Storage Blob SDK failed to load. Please check browser console for details.');
            }
            await new Promise(resolve => setTimeout(resolve, 100));
        }
        console.log('Azure SDK available:', window.Azure.Storage.Blob);
    }

    /**
     * Upload file directly to Azure Blob Storage using SAS URL
     * @param {File} file - The file to upload
     * @param {string} uploadUrl - The SAS URL for upload
     * @param {Function} onProgress - Callback for progress updates (percentage)
     * @param {AbortSignal} signal - Optional abort signal
     * @returns {Promise<void>}
     */
    async uploadFile(file, uploadUrl, onProgress, signal = null) {
        try {
            // Try to use Azure SDK if available, otherwise fall back to native fetch
            if (window.Azure?.Storage?.Blob) {
                await this.uploadWithAzureSDK(file, uploadUrl, onProgress, signal);
            } else {
                console.warn('Azure SDK not available, using native fetch API');
                await this.uploadWithFetch(file, uploadUrl, onProgress, signal);
            }
        } catch (error) {
            if (error.name === 'AbortError') {
                throw new Error('Upload cancelled');
            }
            console.error('Upload error:', error);
            throw new Error(`Upload failed: ${error.message}`);
        }
    }

    /**
     * Upload using Azure SDK (preferred method)
     */
    async uploadWithAzureSDK(file, uploadUrl, onProgress, signal) {
        const { BlockBlobClient } = window.Azure.Storage.Blob;
        this.blockBlobClient = new BlockBlobClient(uploadUrl);

        // Set up abort controller if not provided
        this.abortController = signal ? null : new AbortController();
        const uploadSignal = signal || this.abortController.signal;

        // Configure upload options for optimal performance
        const uploadOptions = {
            blobHTTPHeaders: {
                blobContentType: file.type || 'video/mp4'
            },
            blockSize: 100 * 1024 * 1024, // 100MB blocks
            concurrency: 8, // Upload 8 blocks in parallel
            onProgress: (progressEvent) => {
                if (progressEvent.loadedBytes && file.size) {
                    const percentage = Math.round((progressEvent.loadedBytes / file.size) * 100);
                    if (onProgress) {
                        onProgress(percentage, progressEvent.loadedBytes, file.size);
                    }
                }
            },
            abortSignal: uploadSignal
        };

        await this.blockBlobClient.uploadData(file, uploadOptions);
    }

    /**
     * Upload using native Fetch API (fallback method)
     */
    async uploadWithFetch(file, uploadUrl, onProgress, signal) {
        console.log('Starting upload with XMLHttpRequest');
        console.log('File:', file.name, 'Size:', file.size, 'Type:', file.type);
        console.log('Upload URL (first 100 chars):', uploadUrl.substring(0, 100));
        
        // Set up abort controller if not provided
        this.abortController = signal ? null : new AbortController();
        const uploadSignal = signal || this.abortController.signal;

        // Use XMLHttpRequest for progress tracking (fetch doesn't support upload progress)
        return new Promise((resolve, reject) => {
            const xhr = new XMLHttpRequest();

            xhr.upload.addEventListener('progress', (e) => {
                if (e.lengthComputable && onProgress) {
                    const percentage = Math.round((e.loaded / e.total) * 100);
                    console.log(`Upload progress: ${percentage}% (${e.loaded}/${e.total})`);
                    onProgress(percentage, e.loaded, e.total);
                }
            });

            xhr.addEventListener('load', () => {
                console.log('XHR load event - Status:', xhr.status, 'StatusText:', xhr.statusText);
                if (xhr.status >= 200 && xhr.status < 300) {
                    console.log('Upload completed successfully');
                    resolve();
                } else {
                    console.error('Upload failed:', xhr.status, xhr.statusText);
                    console.error('Response:', xhr.responseText);
                    reject(new Error(`Upload failed with status ${xhr.status}: ${xhr.statusText} - ${xhr.responseText}`));
                }
            });

            xhr.addEventListener('error', (e) => {
                console.error('XHR error event:', e);
                console.error('XHR status:', xhr.status);
                console.error('XHR readyState:', xhr.readyState);
                reject(new Error(`Network error during upload. Status: ${xhr.status}, ReadyState: ${xhr.readyState}. This may be a CORS issue or network connectivity problem.`));
            });

            xhr.addEventListener('abort', () => {
                console.log('XHR aborted');
                reject(new Error('Upload cancelled'));
            });

            // Handle abort signal
            if (uploadSignal) {
                uploadSignal.addEventListener('abort', () => xhr.abort());
            }

            // PUT request to SAS URL
            xhr.open('PUT', uploadUrl);
            xhr.setRequestHeader('x-ms-blob-type', 'BlockBlob');
            xhr.setRequestHeader('Content-Type', file.type || 'video/mp4');
            
            console.log('Sending XHR request...');
            xhr.send(file);
        });
    }

    /**
     * Cancel the current upload
     */
    cancelUpload() {
        if (this.abortController) {
            this.abortController.abort();
        }
    }
}

// Export for use in Blazor components
window.DirectUploadManager = DirectUploadManager;

// Helper function to format file size
window.formatFileSize = function(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
};

// Helper function to format duration
window.formatDuration = function(seconds) {
    if (seconds < 60) return `${Math.round(seconds)}s`;
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = Math.round(seconds % 60);
    return `${minutes}m ${remainingSeconds}s`;
};

// Helper function to calculate upload speed
window.calculateSpeed = function(bytesUploaded, elapsedSeconds) {
    if (elapsedSeconds === 0) return 0;
    return bytesUploaded / elapsedSeconds; // bytes per second
};

