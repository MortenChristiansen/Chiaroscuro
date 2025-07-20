# Middle Mouse Click Implementation

## Overview
This implementation adds support for middle mouse clicks on links to open them in new tabs, which is standard behavior in modern web browsers.

## Architecture

### Components
1. **NavigationBrowserApi** (`src/BrowserHost/Features/Tabs/NavigationBrowserApi.cs`)
   - Provides a C# API method `OpenLinkInNewTab(string url)` that can be called from JavaScript
   - Publishes `NavigationStartedEvent` with `UseCurrentTab: false` to create new tabs

2. **TabBrowser Modifications** (`src/BrowserHost/Features/Tabs/TabBrowser.cs`)
   - Registers the NavigationBrowserApi as a secondary API named "navigationApi"
   - Injects JavaScript on every page load to detect middle mouse clicks
   - Handles the `FrameLoadEnd` event to inject the click detection script

### How It Works

1. **Page Load**: When a page finishes loading, JavaScript is injected into the main frame
2. **Event Listening**: The JavaScript adds a mousedown event listener to the document
3. **Middle Click Detection**: When button 1 (middle mouse) is pressed, the script:
   - Traverses up the DOM tree to find the closest anchor tag
   - Prevents the default link behavior
   - Calls the C# `navigationApi.OpenLinkInNewTab()` method with the link URL
4. **New Tab Creation**: The C# method publishes a NavigationStartedEvent that the TabsFeature handles by creating a new tab

## JavaScript Implementation Details

The injected JavaScript:
- Uses event delegation with capture phase for optimal performance
- Handles nested elements within links (e.g., `<strong>` tags inside `<a>` tags)
- Includes error handling and debugging console output
- Prevents infinite loops when traversing the DOM
- Only processes links with valid href attributes

## Testing

Use the provided test file `test-middle-click.html` to verify:
- Middle mouse clicks open new tabs
- Left clicks work normally (navigate in current tab)
- Right clicks show context menu
- Various link types work correctly (external, anchors, JavaScript, etc.)
- Nested elements within links work
- Console logging shows debug information

## Browser Compatibility

This implementation:
- Works with all standard HTML links (`<a href="...">`)
- Handles relative and absolute URLs
- Works with anchor links, external links, and special protocols
- Compatible with dynamically created links (uses event delegation)
- Does not interfere with existing click handlers or context menus

## Security Considerations

- JavaScript is only injected into main frames, not iframes
- The NavigationBrowserApi only allows opening new tabs, not arbitrary code execution
- Standard web security policies apply to the injected JavaScript
- No user data is exposed to the injected script