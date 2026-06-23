# MeetAmalfiCoast Agent Instructions

## Project Overview

**MeetAmalfiCoast** is a luxury travel experience platform for the Amalfi Coast in Italy. It enables users to browse curated experiences, plan trips, and manage appointments through calendar integration.

**Key characteristics:**
- Mixed Italian/English content (Italian text in UI, English in code)
- Luxury/premium positioning with tailored experiences
- Two main features: Homepage showcase + Planning/Calendar system
- Real-time data synchronization with Firebase

---

## Architecture

### Tech Stack
- **Backend:** ASP.NET Core 8.0 (.NET 8.0)
- **Frontend:** Server-rendered Razor views + vanilla JavaScript/jQuery
- **Database:** Firebase Firestore
- **External APIs:** Google Calendar integration
- **UI Framework:** Bootstrap 5.x, Font Awesome 6.5.2

### Project Structure

```
Controllers/
  ├─ HomeController.cs          # Routes: Index, Privacy, Planning, Error
Models/
  ├─ ErrorViewModel.cs
Views/
  ├─ Home/
  │  ├─ Index.cshtml           # Homepage with hero section & experiences
  │  ├─ Planning.cshtml        # Calendar planning interface
  │  └─ Privacy.cshtml
  └─ Shared/
     ├─ _Layout.cshtml         # Main layout
     └─ _PlanningLayout.cshtml  # Planning-specific layout
wwwroot/
  ├─ css/
  │  ├─ styles.css              # Main styles
  │  ├─ planning.css            # Planning page styles
  │  └─ planning-layout.css     # Planning layout styles
  ├─ js/
  │  ├─ firebaseConfig.js       # Firebase initialization
  │  ├─ planningService.js      # Firebase Firestore operations
  │  ├─ planning.js             # Planning UI logic
  │  └─ script.js               # Global scripts
  └─ lib/
     └─ [Bootstrap, jQuery, jQuery Validation]
Properties/
  └─ launchSettings.json        # Debug profiles
```

---

## Key Features & Files

### 1. **Homepage** (`Views/Home/Index.cshtml`)
- Hero section with Italian copy: "Luxury Experience, Timeless Memories"
- Experiences grid showcasing concierge, activities, etc.
- Styled with premium gold accents and overlays
- Uses Bootstrap + custom CSS

**Edit:** Use gold colors (#d4af37 or similar), maintain Italian descriptions, preserve semantic HTML structure.

### 2. **Planning System** (`Views/Home/Planning.cshtml` + `wwwroot/js/`)
- Calendar view for browsing appointments
- Real-time sync with Firebase Firestore (collection: `appointments`)
- Google Calendar sync capability
- Uses Firestore listeners for live updates

**Key files:**
- `planningService.js` - Firebase operations wrapper (CRUD, listeners, sync)
- `planning.js` - UI event handlers & calendar rendering
- `firebaseConfig.js` - Initializes Firebase client SDK

---

## Development Setup

### Prerequisites
- .NET 8.0 SDK installed
- Node.js/npm (for frontend tools if added in future)
- Firebase project credentials (firebase-service-account.json)

### Build & Run
```bash
dotnet build                    # Build the solution
dotnet run                      # Run development server
dotnet watch run                # Run with auto-reload
```

**Development server:** `https://localhost:7000` (adjust port from launchSettings.json)

### Configuration
- **`appsettings.json`** - Production settings
- **`appsettings.Development.json`** - Dev-specific overrides
- **`firebase-service-account.json`** - Firebase service account credentials (keep private)

---

## Common Patterns & Conventions

### Frontend Patterns

**JavaScript Module Pattern (planningService.js):**
```javascript
const ServiceName = (function() {
  // Private functions
  function privateOperation() { }
  
  // Public API
  return {
    publicMethod: publicOperation
  };
})();
```

**Firestore queries:**
- Collection: `appointments`
- Fields: `isoDate`, `syncStatus`, `createdAt`
- Real-time listeners via `.onSnapshot()`
- Uses ISO date strings for date ranges

**jQuery AJAX:**
```javascript
$.ajax({
  url: "/Controller/Action",
  method: "POST",
  // ...
});
```

### Backend Patterns

**Controller actions:**
- Return `View()` for rendered pages
- Minimal business logic (mostly routing)
- Example: `HomeController` has simple `Index()`, `Planning()`, `Privacy()` actions

**Models:**
- Thin models (ErrorViewModel only)
- Most data flows through Firebase, not server models

---

## Localization & Content

### Italian Content
- Homepage and planning UI use Italian copy
- Examples: "Costiera Amalfitana", "Esperienze esclusive"
- Maintain Italian phrasing when editing marketing copy
- Code comments and variable names remain in English

### Bilingual Handling
- UI text: Italian (user-facing)
- Code: English (developer-facing)
- Keep this separation for maintainability

---

## Firebase Integration

### Firestore Collections
- **`appointments`** - Planning calendar entries
  - Fields: `isoDate`, `syncStatus`, `createdAt`, [other appointment data]
  - Real-time listeners for live updates

### Service Methods (planningService.js)
| Method | Purpose |
|--------|---------|
| `listenAppointmentsByRange(start, end, callback)` | Subscribe to appointments in date range |
| `createAppointment(appointment)` | Add new appointment with sync status |
| `deleteAppointment(id)` | Remove appointment |
| `syncWithGoogleCalendar()` | POST to backend to sync |
| `connectGoogleCalendar()` | Redirect to Google auth flow |

---

## Common Tasks

### Adding a New Page
1. Create `.cshtml` in `Views/Home/`
2. Add action method in `HomeController.cs`
3. Add route in `Program.cs` (if needed)
4. Add stylesheet in `wwwroot/css/` if page-specific styles exist
5. Link in navigation (layouts)

### Modifying Planning Features
1. Update Firestore query/listeners in `planningService.js`
2. Update UI rendering in `planning.js`
3. Ensure `syncStatus: "pending"` workflow is maintained
4. Test with Firebase emulator (future: set up emulator suite)

### Styling
- Use Bootstrap utilities first
- Gold accents: `#d4af37` (confirmed in Index view)
- Dark backgrounds: leverage `section-dark`, `hero-overlay` classes
- Responsive: Mobile-first approach with Bootstrap breakpoints

---

## Performance & Best Practices

- **Lazy Loading:** Consider lazy-loading images in experience cards
- **Firebase Indexes:** Firestore queries on `isoDate` range may need composite index
- **JavaScript Bundles:** Currently no bundling; consider adding Webpack/Vite if >100KB
- **CSS Scoping:** Use layout-specific CSS files to prevent conflicts

---

## Debugging & Troubleshooting

### Firebase Issues
- Check `firebase-service-account.json` is not checked in (verify `.gitignore`)
- Verify Firestore rules allow read/write for appointments collection
- Browser console: Firebase SDK logs detailed errors

### Google Calendar Sync
- OAuth redirect: `/Planning/ConnectGoogleCalendar` must exist on backend
- Sync status tracked in `syncStatus` field (values: "pending", "synced", etc.)

### Build Issues
- Clean: `dotnet clean && dotnet build`
- NuGet restore: `dotnet restore`
- Check .NET 8.0 is installed: `dotnet --version`

---

## AI Agent Guidance

**Before making changes:**
1. Understand the feature scope (Homepage vs Planning)
2. Check if change affects frontend, backend, or both
3. Maintain Italian/English separation in content vs code
4. Verify Firebase field names in Firestore queries
5. Test responsive design on mobile breakpoints

**Common tasks agents handle:**
- ✅ Add new experience cards (HTML + CSS)
- ✅ Modify calendar filtering logic (JavaScript)
- ✅ Adjust styling for premium aesthetic (CSS)
- ✅ Add form validation (jQuery Validation)
- ✅ Debug Firestore query issues
- ❌ Do NOT remove firebase-service-account.json (credentials)
- ❌ Do NOT modify routing without updating Program.cs
