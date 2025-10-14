# Upload Troubleshooting Guide

## Current Issue: Network Error During Upload

### What to Check

1. **Check Browser Console Logs**
   - Open browser DevTools (F12)
   - Go to Console tab
   - Try uploading again
   - Look for these log messages:
     - "Starting upload with XMLHttpRequest"
     - "Upload URL (first 100 chars): ..."
     - "XHR error event" with status and readyState
   
   **Please share these console logs** - they will tell us exactly what's failing.

2. **Check if CORS is Configured**
   
   The CORS configuration we added needs to be deployed to Azure Storage. If you haven't deployed since adding CORS:
   
   ```bash
   azd deploy
   ```

3. **Check Network Tab**
   - In DevTools, go to Network tab
   - Try uploading
   - Look for a PUT request to blob.core.windows.net
   - Check if it shows CORS error or other failure
   - Right-click the failed request → Copy → Copy as cURL

### Common Issues

#### Issue 1: CORS Not Configured (Most Likely)
**Symptoms:**
- Network error with status 0
- Console shows CORS policy error
- Request never reaches server

**Solution:**
Deploy the infrastructure changes:
```bash
azd deploy
```

This applies the CORS configuration in `infra/storage/storage.module.bicep`.

#### Issue 2: Testing Locally with Azurite
**Symptoms:**
- Upload URL contains `127.0.0.1` or `localhost`
- Same CORS error

**Solution for Azurite:**
Azurite needs to be started with CORS enabled:
```bash
azurite --loose --blobHost 127.0.0.1 --blobPort 10000 --cors "*"
```

Or if using Docker:
```bash
docker run -p 10000:10000 mcr.microsoft.com/azure-storage/azurite azurite-blob --blobHost 0.0.0.0 --loose --cors "*"
```

#### Issue 3: Storage Container Doesn't Exist
**Symptoms:**
- Upload URL looks valid
- Error status 404

**Solution:**
The container should be created automatically, but verify:
1. Check Azure Portal → Storage Account → Containers
2. Ensure "videos" container exists
3. Check that the managed identity has permissions

#### Issue 4: SAS Token Invalid
**Symptoms:**
- Error status 403 (Forbidden)
- Error message about authentication

**Solution:**
- Check server logs for SAS generation errors
- Verify managed identity has "Storage Blob Data Contributor" role
- Check that storage account allows shared key access for dev, or uses managed identity for production

### Quick Test: Check Upload URL

Add this to your browser console when the error occurs:

```javascript
// This will be in the logs already, but you can also check:
console.log('Can we reach storage?');
fetch('https://your-storage-account.blob.core.windows.net/', {
  method: 'OPTIONS',
  headers: {
    'Origin': window.location.origin,
    'Access-Control-Request-Method': 'PUT'
  }
}).then(r => console.log('Preflight response:', r.status, r.headers))
  .catch(e => console.error('Preflight failed:', e));
```

### Temporary Workaround: Use Legacy Upload

While debugging, you can temporarily revert to the legacy upload that goes through the server:

1. Comment out the interactive render mode:
   ```razor
   @* @rendermode InteractiveServer *@
   ```

2. Use the old controller-based upload form

Or create a simple test page that uses the legacy `VideoUploadController`.

### Next Steps

1. **Try uploading again** with DevTools console open
2. **Share the console logs** - especially:
   - The upload URL (first 100 chars)
   - XHR status and readyState
   - Any CORS errors
3. **Check if deployed to Azure** or testing locally
4. **If local**, ensure Azurite is running with CORS enabled
5. **If Azure**, run `azd deploy` to apply CORS config

### Expected Console Output (Success)

When working correctly, you should see:
```
Azure SDK not available, using native fetch API
Starting upload with XMLHttpRequest
File: myvideo.mp4 Size: 12345678 Type: video/mp4
Upload URL (first 100 chars): https://storagexxxxx.blob.core.windows.net/videos/guid_myvideo.mp4?sv=2021...
Sending XHR request...
Upload progress: 10% (1234567/12345678)
Upload progress: 25% (3086419/12345678)
...
Upload progress: 100% (12345678/12345678)
XHR load event - Status: 201 StatusText: Created
Upload completed successfully
```

### Contact Info

Once you have the console logs, we can pinpoint the exact issue and fix it.

