const PROFILE_STORAGE_KEY = "attractions_participant_profile_v1";
const PACKAGE_STORAGE_KEY = "attractions_active_package_v1";

function loadSessionJson(key) {
  try {
    const raw = sessionStorage.getItem(key);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

function saveSessionJson(key, value) {
  try {
    if (value === null || value === undefined) {
      sessionStorage.removeItem(key);
    } else {
      sessionStorage.setItem(key, JSON.stringify(value));
    }
  } catch {
    // ignore storage quota errors
  }
}

export const state = {
  role: "user", // "user" | "organizer"
  userTab: "catalog", // "catalog" | "builder" | "profile" | "orders"
  organizerTab: "attractions", // "attractions" | "offers" | "addonsRelations" | "features" | "orders"

  features: [],
  attractions: [],

  // User Catalog Filters
  catalogFilter: "available", // "available" | "today" | "group" | "group4"
  selectedAttractionId: null,
  onlyCompatibleOffers: false,
  groupSizeFilter: "",
  searchQuery: "",

  // Organizer Offers Filter
  orgOfferFilter: "all", // "all" | "planned" | "today" | "withdrawn" | "group"
  selectedOrgOfferIdForExtras: null,

  // Active Participant Profile
  profile: loadSessionJson(PROFILE_STORAGE_KEY),

  // Active Package Configuration: { baseOfferId, selectedAddonIds: [], relatedOffers: [{ offerId, selectedAddonIds: [] }] }
  packageConfig: loadSessionJson(PACKAGE_STORAGE_KEY),
  packageEvaluation: null,
  customGroupBreakdown: null,

  setProfile(newProfile) {
    this.profile = newProfile;
    saveSessionJson(PROFILE_STORAGE_KEY, newProfile);
  },

  setPackageConfig(newConfig) {
    this.packageConfig = newConfig;
    saveSessionJson(PACKAGE_STORAGE_KEY, newConfig);
  }
};
