// Tribute.js integration for mention/autocomplete functionality

let tributeInstances = {};

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
        values: mentionItems.map(item => ({
            key: item.name,
            value: item.name,
            id: item.id,
            avatar: item.avatar,
            subtitle: item.subtitle
        })),
        selectTemplate: function (item) {
            // Check if we're using contenteditable
            if (element.getAttribute('contenteditable')) {
                // Return styled HTML for contenteditable - use regular space, not &nbsp;
                return '<span class="mention-tag" contenteditable="false" data-mention-id="' + item.original.id + '" data-mention-name="' + item.original.value + '">@' + item.original.value + '</span> ';
            } else {
                // Return plain text for textarea
                return '@' + item.original.value + ' ';
            }
        },
        menuItemTemplate: function (item) {
            let html = '<div class="flex items-center gap-2 py-2">';
            
            if (item.original.avatar) {
                html += `<img src="${item.original.avatar}" class="w-8 h-8 rounded-full" alt="${item.original.value}" />`;
            } else {
                // Generate initials
                const initials = getInitials(item.original.value);
                html += `<div class="w-8 h-8 rounded-full bg-blue-600 text-white flex items-center justify-center text-xs font-semibold">${initials}</div>`;
            }
            
            html += '<div class="flex flex-col">';
            html += `<span class="text-sm font-medium text-gray-900 dark:text-gray-100">${item.original.value}</span>`;
            
            if (item.original.subtitle) {
                html += `<span class="text-xs text-gray-500 dark:text-gray-400">${item.original.subtitle}</span>`;
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

    // Listen for input changes to sync with Blazor
    element.addEventListener('input', async (e) => {
        try {
            let value;
            let mentions = [];
            
            if (element.getAttribute('contenteditable')) {
                // For contenteditable, get the text content (without HTML)
                value = getTextFromContentEditable(element);
                // Extract mention metadata from the DOM
                mentions = extractMentionsFromDOM(element);
                console.log('Extracted mentions from DOM:', mentions);
            } else {
                // For textarea, use the value
                value = e.target.value;
            }
            
            try {
                await dotNetRef.invokeMethodAsync('OnTextChanged', value);
                console.log('OnTextChanged succeeded');
            } catch (e) {
                console.error('OnTextChanged failed:', e);
            }
            
            // Update mentions metadata (always, even if empty, to keep in sync)
            if (element.getAttribute('contenteditable')) {
                console.log('Calling OnMentionsChanged with:', mentions);
                try {
                    await dotNetRef.invokeMethodAsync('OnMentionsChanged', mentions);
                    console.log('OnMentionsChanged succeeded');
                } catch (e) {
                    console.error('OnMentionsChanged failed:', e);
                }
            }
        } catch (error) {
            console.error('Error invoking callbacks:', error);
        }
    });

    console.log(`Tribute.js initialized for ${elementId}`);
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
                
                // Use PascalCase to match C# class properties
                mentions.push({
                    Id: mentionId,
                    Name: mentionName,
                    Position: currentPosition,
                    Length: mentionText.length
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

