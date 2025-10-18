// Tribute.js integration for mention/autocomplete functionality

let tributeInstances = {};

// Security: HTML escape helper to prevent XSS attacks
function escapeHtml(text) {
    if (text == null) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Security: Sanitize URLs to prevent javascript: protocol and other XSS vectors
function sanitizeUrl(url) {
    if (!url) return '';
    
    // Remove whitespace
    url = url.trim();
    
    // Check for dangerous protocols
    const dangerousProtocols = /^(javascript|data|vbscript|file|about):/i;
    if (dangerousProtocols.test(url)) {
        console.warn('Blocked potentially dangerous URL:', url);
        return '';
    }
    
    // Only allow http, https, or relative URLs
    if (!/^(https?:)?\/\//i.test(url) && !url.startsWith('/')) {
        console.warn('Invalid URL protocol:', url);
        return '';
    }
    
    return escapeHtml(url);
}

export function initializeTribute(elementId, dotNetRef, mentionItems) {
    const element = document.getElementById(elementId);
    if (!element) {
        console.error(`Element with id ${elementId} not found`);
        return;
    }

    // Check if Tribute is loaded
    if (typeof Tribute === 'undefined') {
        console.error('Tribute.js is not loaded. Make sure to include it in your layout.');
        return;
    }

    const tribute = new Tribute({
        values: async function (text, callback) {
            // Filter the mention items
            const filtered = mentionItems
                .filter(item => item.name.toLowerCase().includes(text.toLowerCase()))
                .map(item => ({
                    key: item.name,
                    value: item.name,
                    id: item.id,
                    avatar: item.avatar,
                    subtitle: item.subtitle,
                    isExisting: true
                }));

            // If there's text and no exact match, add "Create new person" option
            if (text.trim().length > 0) {
                const exactMatch = filtered.some(item => 
                    item.value.toLowerCase() === text.toLowerCase()
                );
                
                if (!exactMatch) {
                    filtered.push({
                        key: text,
                        value: text,
                        id: 'new-person',
                        avatar: null,
                        subtitle: 'Create new person',
                        isExisting: false
                    });
                }
            }

            callback(filtered);
        },
        selectTemplate: function (item) {
            let personId = escapeHtml(item.original.id);
            let personName = escapeHtml(item.original.value);
            
            // Check if we're using contenteditable
            if (element.getAttribute('contenteditable')) {
                // Return styled HTML for contenteditable - use regular space, not &nbsp;
                // If this is a new person, use a temporary ID that will be updated
                return '<span class="mention-tag" contenteditable="false" data-mention-id="' + personId + '" data-mention-name="' + personName + '" data-is-new="' + (!item.original.isExisting) + '">@' + personName + '</span> ';
            } else {
                // Return plain text for textarea - no escaping needed for plain text
                return '@' + item.original.value + ' ';
            }
        },
        menuItemTemplate: function (item) {
            // Escape all user-controlled values to prevent XSS
            const escapedValue = escapeHtml(item.original.value);
            const escapedSubtitle = escapeHtml(item.original.subtitle);
            
            let html = '<div class="flex items-center gap-2 py-2">';
            
            if (!item.original.isExisting) {
                // Show a "+" icon for creating new person
                html += `<div class="w-8 h-8 rounded-full bg-green-600 text-white flex items-center justify-center text-lg font-semibold">+</div>`;
            } else if (item.original.avatar) {
                const sanitizedAvatar = sanitizeUrl(item.original.avatar);
                if (sanitizedAvatar) {
                    html += `<img src="${sanitizedAvatar}" class="w-8 h-8 rounded-full" alt="${escapedValue}" />`;
                } else {
                    // Fall back to initials if avatar URL is invalid
                    const initials = escapeHtml(getInitials(item.original.value));
                    html += `<div class="w-8 h-8 rounded-full bg-blue-600 text-white flex items-center justify-center text-xs font-semibold">${initials}</div>`;
                }
            } else {
                // Generate initials
                const initials = escapeHtml(getInitials(item.original.value));
                html += `<div class="w-8 h-8 rounded-full bg-blue-600 text-white flex items-center justify-center text-xs font-semibold">${initials}</div>`;
            }
            
            html += '<div class="flex flex-col">';
            html += `<span class="text-sm font-medium text-gray-900 dark:text-gray-100">${escapedValue}</span>`;
            
            if (item.original.subtitle) {
                const subtitleClass = !item.original.isExisting 
                    ? 'text-xs text-green-600 dark:text-green-400 font-medium'
                    : 'text-xs text-gray-500 dark:text-gray-400';
                html += `<span class="${subtitleClass}">${escapedSubtitle}</span>`;
            }
            
            html += '</div></div>';
            
            return html;
        },
        lookup: 'value',
        fillAttr: 'value',
        requireLeadingSpace: true,
        allowSpaces: true,
        menuShowMinLength: 0,
        noMatchTemplate: function() {
            return null;
        }
    });

    tribute.attach(element);
    tributeInstances[elementId] = tribute;

    // Debounce timer for input changes
    let debounceTimer = null;
    const DEBOUNCE_MS = 150; // Adjust this value to tune responsiveness vs. performance

    // Listen for input changes to sync with Blazor
    element.addEventListener('input', async (e) => {
        try {
            // Clear any pending debounced call
            if (debounceTimer) {
                clearTimeout(debounceTimer);
            }

            // Debounce the interop call to reduce overhead during fast typing
            debounceTimer = setTimeout(async () => {
                try {
                    let value;
                    let mentions = null;
                    
                    if (element.getAttribute('contenteditable')) {
                        // For contenteditable, get the text content (without HTML)
                        value = getTextFromContentEditable(element);
                        // Extract mention metadata from the DOM
                        mentions = extractMentionsFromDOM(element);
                    } else {
                        // For textarea, use the value
                        value = e.target.value;
                    }
                    
                    // Batch both updates into a single JS interop call for better performance
                    try {
                        await dotNetRef.invokeMethodAsync('OnInputChanged', value, mentions);
                    } catch (e) {
                        console.error('OnInputChanged failed:', e);
                    }
                } catch (error) {
                    console.error('Error invoking callbacks:', error);
                }
            }, DEBOUNCE_MS);
        } catch (error) {
            console.error('Error setting up debounce:', error);
        }
    });
}

export function getContentEditableText(elementId) {
    const element = document.getElementById(elementId);
    if (!element) return '';
    return getTextFromContentEditable(element);
}

function getTextFromContentEditable(element) {
    // Extract plain text from contenteditable, converting mention spans to @mentions
    let text = '';
    
    function processNode(node) {
        if (node.nodeType === Node.TEXT_NODE) {
            text += node.textContent;
        } else if (node.nodeType === Node.ELEMENT_NODE) {
            if (node.classList.contains('mention-tag')) {
                // Extract the @mention text from the span
                text += node.textContent;
            } else if (node.tagName === 'BR') {
                text += '\n';
            } else {
                // Recurse into child nodes
                node.childNodes.forEach(processNode);
            }
        }
    }
    
    element.childNodes.forEach(processNode);
    return text;
}

function extractMentionsFromDOM(element) {
    // Extract mention metadata by walking the DOM and tracking text positions
    const mentions = [];
    let currentPosition = 0;
    
    function processNode(node) {
        if (node.nodeType === Node.TEXT_NODE) {
            // Regular text, just advance the position
            currentPosition += node.textContent.length;
        } else if (node.nodeType === Node.ELEMENT_NODE) {
            if (node.classList.contains('mention-tag')) {
                // This is a mention!
                const mentionText = node.textContent;
                const mentionId = node.getAttribute('data-mention-id');
                const mentionName = node.getAttribute('data-mention-name');
                const isNew = node.getAttribute('data-is-new') === 'true';
                
                // Use PascalCase to match C# class properties
                mentions.push({
                    Id: mentionId,
                    Name: mentionName,
                    Position: currentPosition,
                    Length: mentionText.length,
                    IsNewPerson: isNew
                });
                
                currentPosition += mentionText.length;
            } else if (node.tagName === 'BR') {
                currentPosition += 1; // newline
            } else {
                // Recurse into child nodes
                node.childNodes.forEach(processNode);
            }
        }
    }
    
    element.childNodes.forEach(processNode);
    return mentions;
}

export function disposeTribute(elementId) {
    const tribute = tributeInstances[elementId];
    if (tribute) {
        const element = document.getElementById(elementId);
        if (element) {
            tribute.detach(element);
        }
        delete tributeInstances[elementId];
    }
}

function getInitials(name) {
    const parts = name.trim().split(/\s+/);
    if (parts.length === 0) return '?';
    if (parts.length === 1) return parts[0][0].toUpperCase();
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

