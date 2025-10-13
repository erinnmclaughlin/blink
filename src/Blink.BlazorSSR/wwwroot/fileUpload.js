// JavaScript interop for large file uploads using Fetch API
// This avoids Blazor's HttpClient memory buffering issues

window.uploadLargeFile = async function (url, fileInput, authToken, title, description, videoDate) {
    try {
        const file = fileInput.files[0];
        if (!file) {
            throw new Error('No file selected');
        }

        const formData = new FormData();
        
        // IMPORTANT: Add metadata fields BEFORE the file
        // The server reads form fields first, then the file stream
        if (title) {
            formData.append('title', title);
        }
        if (description) {
            formData.append('description', description);
        }
        if (videoDate) {
            formData.append('videoDate', videoDate);
        }
        
        // Add the video file last
        formData.append('video', file);

        const headers = {
            'Authorization': `Bearer ${authToken}`
        };

        console.log('Uploading to:', url);
        console.log('File:', file.name, 'Size:', file.size);

        const response = await fetch(url, {
            method: 'POST',
            headers: headers,
            body: formData
        });

        console.log('Response status:', response.status, response.statusText);

        if (!response.ok) {
            let errorText = '';
            let errorMessage = '';
            
            try {
                errorText = await response.text();
                console.log('Error response text:', errorText);
            } catch (e) {
                console.error('Failed to read error text:', e);
            }

            if (errorText) {
                try {
                    const errorJson = JSON.parse(errorText);
                    errorMessage = errorJson.error || errorJson.title || errorJson.detail || JSON.stringify(errorJson);
                } catch {
                    errorMessage = errorText;
                }
            }
            
            if (!errorMessage) {
                errorMessage = `${response.status} ${response.statusText}`;
            }
            
            throw new Error(`Upload failed (${response.status}): ${errorMessage}`);
        }

        const result = await response.json();
        console.log('Upload successful:', result);
        return result;
    } catch (error) {
        console.error('Upload error:', error);
        throw error;
    }
};

// Helper function to get file info without reading the whole file
window.getFileInfo = function (fileInput) {
    const file = fileInput.files[0];
    if (!file) {
        return null;
    }
    
    return {
        name: file.name,
        size: file.size,
        type: file.type
    };
};

