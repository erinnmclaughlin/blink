# MentionTextarea Component - Tribute.js Integration

## Overview
This is a proof-of-concept Blazor component that integrates Tribute.js to provide @-mention functionality in text areas. It's similar to the mention features in Slack, GitHub, Discord, etc.

## Files Created

### 1. **MentionTextarea.razor**
   - Location: `src/Blink.Web/Blink.Web/Components/Shared/`
   - Main Blazor component with two-way data binding
   - Supports customizable CSS, placeholder text, rows, etc.
   - Implements `IAsyncDisposable` for proper cleanup

### 2. **MentionTextarea.razor.js**
   - JavaScript interop file for Tribute.js integration
   - Handles initialization, event binding, and cleanup
   - Generates initials automatically if no avatar is provided

### 3. **Styles/main.css**
   - Custom styling for Tribute.js dropdown to match dark/light themes
   - Uses Tailwind CSS classes for consistency
   - Styles are processed by Tailwind and compiled into `wwwroot/main.css`
## Integration Points

### Dependencies Added
- **App.razor**: Added Tribute.js CDN links (CSS and JS)
  ```html
  <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/tributejs@5.1.3/dist/tribute.css" />
  <script src="https://cdn.jsdelivr.net/npm/tributejs@5.1.3/dist/tribute.min.js"></script>
  ```

### Styles
- **Styles/main.css**: Added custom Tribute.js styles using Tailwind's `@apply` directives
  - Styles are processed by Tailwind and compiled into `wwwroot/main.css`
  - Includes both light and dark mode support using `dark:` variants
  - Uses media query-based dark mode (`prefers-color-scheme`)

### Namespace Import
- **_Imports.razor**: Added `@using Blink.Web.Components.Shared` for component availability

### Example Usage
- **VideoDetailPage.razor**: Replaced the static comment textarea with `MentionTextarea`
- **VideoDetailPage.razor.cs**: Added sample mentionable people and event handlers

## How to Use

### Basic Usage

```razor
<MentionTextarea Value="@myText"
                 ValueChanged="@OnTextChanged"
                 Placeholder="Type @ to mention someone"
                 Rows="3"
                 MentionItems="@people" />
```

### Data Structure

```csharp
private readonly List<MentionTextarea.MentionItem> people = new()
{
    new() { Id = "1", Name = "Erin McLaughlin", Subtitle = "Team Lead" },
    new() { Id = "2", Name = "John Doe", Subtitle = "Developer" }
};
```

## Testing the POC

1. **Stop the currently running application**
2. **Rebuild the project**: `dotnet build`
3. **Run the application**
4. **Navigate to any video detail page** (e.g., `/video/{some-guid}`)
5. **Scroll down to the Comments section**
6. **Type `@` in the comment textarea**
7. **You should see a dropdown with mentionable people**
8. **Use arrow keys to navigate, Enter to select, or click an item**

## Features

✅ **@-mention trigger**: Type `@` to show the dropdown  
✅ **Typeahead search**: Filter people as you type  
✅ **Keyboard navigation**: Arrow keys + Enter  
✅ **Mouse selection**: Click to select  
✅ **Initials generation**: Auto-generates initials from names  
✅ **Dark mode support**: Styled to match your theme  
✅ **Accessible**: Proper ARIA attributes  
✅ **Reusable**: Can be used anywhere in the app  

## Current Implementation

The component is currently integrated into:
- **Comment textarea** on the video detail page (line 109-113 of VideoDetailPage.razor)

It replaces the static textarea with full mention functionality.

## Next Steps / Production Considerations

1. **Data Source**: Replace the hardcoded `mentionablePeople` list with real data from your user service
2. **Persistence**: 
   - Store BOTH text and metadata in database
   - Add JSON column for mention metadata: `DescriptionMentions`, `CommentMentions`
   - Example: `{"id":"1","name":"Erin McLaughlin","position":13,"length":16}`
3. **Display**: 
   - Load mention metadata from database
   - Pass to `<MentionText>` component for rendering
   - Make mentions clickable links to user profiles
4. **Notifications**: Notify users when they're mentioned
5. **Permissions**: Filter mentionable users based on video/org permissions
6. **Performance**: For large user lists, implement server-side search via API
7. **Testing**: Add unit and integration tests

## Architecture

### Metadata-Based Approach
- **No regex parsing** - mentions use structured metadata for 100% accuracy
- **On input**: JavaScript walks the DOM and extracts mention positions
- **On save**: Store both text and JSON metadata
- **On display**: Use metadata to render mentions precisely

### Data Flow
1. User types `@` → Tribute.js shows dropdown
2. User selects person → Mention span inserted into contenteditable
3. On change → `extractMentionsFromDOM()` walks DOM and captures metadata
4. On save → Store text + JSON metadata in database
5. On display → `<MentionText>` uses metadata to render badges

### Why No Backwards Compatibility?
- Simpler, cleaner code
- No fragile regex patterns
- Clear contract: mentions require metadata
- If metadata missing, text displays without badges

## Troubleshooting

**Dropdown doesn't appear:**
- Check browser console for JavaScript errors
- Verify Tribute.js is loaded (check Network tab)
- Ensure `MentionItems` prop is populated

**Styling issues:**
- Check if dark mode is working correctly
- Verify Tailwind CSS is compiling the custom styles
- Inspect the `.tribute-container` element

**Data binding issues:**
- Ensure `ValueChanged` callback is implemented
- Check if the component has `@rendermode InteractiveServer`

