// Helper functions for file handling in the upload page

window.uploadFileDirectly = async function(inputId, uploadUrl, dotNetReference, callbackMethod) {
    // InputFile component creates an input with the given id
    let input = document.getElementById(inputId);
    
    // If not found by id, try to find the input element within the component
    if (!input) {
        const label = document.querySelector(`label[for="${inputId}"]`);
        if (label && label.nextElementSibling) {
            input = label.nextElementSibling.querySelector('input[type="file"]');
        }
    }
    
    // Last resort: find any file input on the page (for InputFile component)
    if (!input) {
        input = document.querySelector('input[type="file"]');
    }
    
    if (!input || !input.files || input.files.length === 0) {
        throw new Error('No file selected or file input not found');
    }
    
    const file = input.files[0];
    
    try {
        // Create upload manager
        const uploadManager = new window.DirectUploadManager();
        
        // Define progress callback that invokes .NET method
        const onProgress = (percentage, bytesUploaded, totalBytes) => {
            dotNetReference.invokeMethodAsync(callbackMethod, percentage, bytesUploaded, totalBytes);
        };
        
        // Upload the file
        await uploadManager.uploadFile(file, uploadUrl, onProgress);
        
    } catch (error) {
        console.error('Upload error:', error);
        throw error;
    }
};

