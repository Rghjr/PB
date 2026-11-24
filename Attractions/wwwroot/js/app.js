import { api } from "./api.js";
import { state } from "./state.js";
import {
  esc,
  money,
  showToast,
  openModal,
  closeModal,
  renderCompatibilityBox,
  renderOrderReceiptHtml
} from "./ui.js";
import { renderOrganizerView } from "./organizer.js";

const USER_TABS = [
  { id: "catalog", label: "1. Katalog atrakcji i ofert" },
  { id: "builder", label: "2. Konfigurator pakietu i zakup" },
  { id: "profile", label: "3. Profil uczestnika / grupy" },
  { id: "orders", label: "4. Moje zamówienia" }
];

const ORGANIZER_TABS = [
  { id: "attractions", label: "Atrakcje i limity osób" },
  { id: "offers", label: "Oferty i trasy grupowe" },
  { id: "addonsRelations", label: "Dodatki i powiązania N:M" },
  { id: "features", label: "Słownik cech (Features.json)" },
  { id: "orders", label: "Zamówienia klientów" }
];

document.addEventListener("DOMContentLoaded", async () => {
  bindShellEvents();
  await refreshGlobalData();
  await refreshPackageEvaluation();
  await renderApp();
});

document.addEventListener("app:rerender", async () => {
  await refreshPackageEvaluation();
  await renderApp();
});

function bindShellEvents() {
  document.getElementById("modalCloseBtn").addEventListener("click", closeModal);
  document.getElementById("modalBackdrop").addEventListener("click", (e) => {
    if (e.target.id === "modalBackdrop") closeModal();
  });

  document.getElementById("brandHomeBtn").addEventListener("click", async () => {
    state.role = "user";
    state.userTab = "catalog";
    await renderApp();
  });

  document.getElementById("headerProfilePill").addEventListener("click", async () => {
    state.role = "user";
    state.userTab = "profile";
    await renderApp();
  });

  document.getElementById("headerCartPill").addEventListener("click", async () => {
    state.role = "user";
    state.userTab = "builder";
    await renderApp();
  });

  document.getElementById("roleUserBtn").addEventListener("click", async () => {
    state.role = "user";
    await renderApp();
  });

  document.getElementById("roleOrganizerBtn").addEventListener("click", async () => {
    state.role = "organizer";
    await renderApp();
  });
}

async function refreshGlobalData() {
  try {
    const [features, attractions] = await Promise.all([
      api.getFeatures(),
      api.getAttractions(false)
    ]);
    state.features = features;
    state.attractions = attractions;
  } catch (err) {
    showToast(`Błąd ładowania danych: ${err.message}`, "error");
  }
}

async function refreshPackageEvaluation() {
  if (!state.packageConfig?.baseOfferId) {
    state.packageEvaluation = null;
    updateHeaderPills();
    return;
  }

  try {
    state.packageEvaluation = await api.evaluatePackage({
      configuration: state.packageConfig,
      profile: state.profile,
      groupBreakdown: state.customGroupBreakdown
    });
  } catch {
    state.setPackageConfig(null);
    state.packageEvaluation = null;
    state.customGroupBreakdown = null;
  }
  updateHeaderPills();
}

function updateHeaderPills() {
  document.getElementById("roleUserBtn").classList.toggle("active", state.role === "user");
  document.getElementById("roleOrganizerBtn").classList.toggle("active", state.role === "organizer");

  const dot = document.getElementById("headerProfileDot");
  const profileTxt = document.getElementById("headerProfileText");

  if (state.profile && state.profile.participantAges?.length) {
    dot.classList.add("active");
    const ages = state.profile.participantAges;
    const grpLabel = ages.length > 1 ? `Grupa ${ages.length} os. (${ages.join(", ")} lat)` : `1 os. (${ages[0]} lat)`;
    const extras = [];
    if (state.profile.requiresWheelchairAccess) extras.push("Wózek: Tak");
    if (state.profile.maxPhysicalDemand) extras.push(`Wysiłek: do ${state.profile.maxPhysicalDemand}`);
    profileTxt.textContent = `Profil: ${grpLabel}${extras.length ? " • " + extras.join(" • ") : ""}`;
  } else {
    dot.classList.remove("active");
    profileTxt.textContent = "Profil uczestnika: nie ustawiono (kliknij)";
  }

  const cartPill = document.getElementById("headerCartPill");
  const cartTxt = document.getElementById("headerCartText");
  if (state.packageEvaluation) {
    cartPill.classList.remove("hidden");
    const ev = state.packageEvaluation;
    const isIncompat = ev.compatibility && !ev.compatibility.isCompatible;
    cartPill.classList.toggle("incompatible", Boolean(isIncompat));
    const compatSuffix = ev.compatibility ? (ev.compatibility.isCompatible ? " • Zgodny" : " • Niezgodny (!)") : "";
    cartTxt.textContent = `Pakiet: ${ev.baseOfferName.slice(0, 26)}${ev.baseOfferName.length > 26 ? "…" : ""} (${money(ev.priceBreakdown.finalTotalPrice)} za ${ev.priceBreakdown.totalParticipants} os.${compatSuffix})`;
  } else {
    cartPill.classList.add("hidden");
  }
}

async function renderApp() {
  updateHeaderPills();
  renderSubnav();

  const mainView = document.getElementById("mainView");
  if (state.role === "organizer") {
    await renderOrganizerView(mainView, refreshGlobalData);
    return;
  }

  switch (state.userTab) {
    case "catalog":
      await renderUserCatalog(mainView);
      break;
    case "builder":
      await renderPackageBuilder(mainView);
      break;
    case "profile":
      await renderProfileTab(mainView);
      break;
    case "orders":
      await renderUserOrdersTab(mainView);
      break;
  }
}

function renderSubnav() {
  const nav = document.getElementById("subnavTabs");
  const tabs = state.role === "user" ? USER_TABS : ORGANIZER_TABS;
  const current = state.role === "user" ? state.userTab : state.organizerTab;

  nav.innerHTML = tabs
    .map(
      (t) => `<button type="button" class="tab-btn ${t.id === current ? "active" : ""}" data-tab="${t.id}">${esc(t.label)}</button>`
    )
    .join("");

  nav.querySelectorAll("[data-tab]").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const target = btn.getAttribute("data-tab");
      if (state.role === "user") state.userTab = target;
      else state.organizerTab = target;
      await renderApp();
    });
  });
}

// ============================================================================
// 1. KATALOG ATRAKCJI I OFERT (UŻYTKOWNIK)
// ============================================================================

async function renderUserCatalog(container) {
  const searchPayload = {
    filter: state.catalogFilter,
    attractionId: state.selectedAttractionId,
    groupSize: state.groupSizeFilter ? Number(state.groupSizeFilter) : null,
    onlyCompatible: state.onlyCompatibleOffers,
    profile: state.profile
  };

  const [offers, availableAttractions] = await Promise.all([
    api.searchOffers(searchPayload),
    api.getAttractions(true)
  ]);

  const queryLower = state.searchQuery.trim().toLowerCase();
  const visibleOffers = queryLower
    ? offers.filter(
        (o) =>
          o.name.toLowerCase().includes(queryLower) ||
          o.description.toLowerCase().includes(queryLower) ||
          o.attractionName.toLowerCase().includes(queryLower)
      )
    : offers;

  const filterModes = [
    { id: "available", label: "Wszystkie dostępne" },
    { id: "today", label: "Na dzisiaj" },
    { id: "group", label: "Tylko oferty grupowe" },
    { id: "group4", label: "Tylko grupy 4-osobowe" }
  ];

  container.innerHTML = `
    <div class="section-header">
      <div>
        <h2 class="section-title">Atrakcje turystyczne i oferty zwiedzania</h2>
        <p class="section-desc">Wybierz atrakcję, filtruj trasy indywidualne lub grupowe (np. 4-osobowe) oraz weryfikuj zgodność z profilem uczestnika.</p>
      </div>
      <div class="btn-row">
        <button class="btn" id="openQuickProfileBtn">
          ${state.profile ? "Edytuj profil uczestnika / grupy" : "Skonfiguruj profil uczestnika / grupy"}
        </button>
      </div>
    </div>

    <details class="panel" ${state.selectedAttractionId ? "open" : ""}>
      <summary style="cursor:pointer;font-weight:800;font-size:18px;color:var(--blue-deep);">
        Wybór wg atrakcji turystycznej (${availableAttractions.length} obiektów z aktywnymi ofertami)
        ${
          state.selectedAttractionId
            ? ` — wybrano #${state.selectedAttractionId} <button type="button" class="btn btn-sm" id="clearAttrFilterBtn" style="margin-left:8px;">Pokaż wszystkie atrakcje</button>`
            : ""
        }
      </summary>
      <div class="attractions-strip" style="margin-top:14px;margin-bottom:0;">
        ${availableAttractions
          .map(
            (a) => `
          <div class="attraction-mini-card ${state.selectedAttractionId === a.id ? "selected" : ""}" data-pick-attr="${a.id}">
            <div>
              <div class="attraction-mini-name">#${a.id} ${esc(a.name)}</div>
              <div class="form-hint">${esc(a.location)}</div>
            </div>
            <div class="attraction-mini-meta">
              <span>Maks. ${a.maxParticipants} os. • Oferty: ${a.availableOffersCount}</span>
              <button type="button" class="btn btn-sm" data-check-attr="${a.id}">Dostępność</button>
            </div>
          </div>
        `
          )
          .join("")}
      </div>
    </details>

    <div class="filters-bar">
      <div class="filter-chips">
        ${filterModes
          .map(
            (m) => `<button type="button" class="chip ${state.catalogFilter === m.id ? "active" : ""}" data-cat-filter="${m.id}">${esc(m.label)}</button>`
          )
          .join("")}
      </div>

      <input type="text" id="catalogSearchInput" placeholder="Szukaj trasy lub atrakcji..." value="${esc(state.searchQuery)}" style="max-width:290px;" />

      <input type="number" min="1" max="100" id="catalogGroupSizeInput" placeholder="Liczba osób (np. 4)" value="${esc(state.groupSizeFilter)}" style="max-width:210px;" />

      <label class="checkbox-line" style="margin-left:auto;">
        <input type="checkbox" id="onlyCompatibleCheckbox" ${state.onlyCompatibleOffers ? "checked" : ""} />
        <span>Tylko zgodne z moim profilem</span>
      </label>
    </div>

    ${
      visibleOffers.length === 0
        ? `<div class="panel">Brak ofert spełniających wybrane kryteria filtrowania.</div>`
        : `<div class="offers-grid">${visibleOffers.map(renderOfferCardHtml).join("")}</div>`
    }
  `;

  container.querySelector("#openQuickProfileBtn").addEventListener("click", () => {
    state.userTab = "profile";
    renderApp();
  });

  const clearAttrBtn = container.querySelector("#clearAttrFilterBtn");
  if (clearAttrBtn) {
    clearAttrBtn.addEventListener("click", (e) => {
      e.preventDefault();
      e.stopPropagation();
      state.selectedAttractionId = null;
      renderUserCatalog(container);
    });
  }

  container.querySelectorAll("[data-pick-attr]").forEach((card) => {
    card.addEventListener("click", (e) => {
      if (e.target.closest("[data-check-attr]")) return;
      const id = Number(card.getAttribute("data-pick-attr"));
      state.selectedAttractionId = state.selectedAttractionId === id ? null : id;
      renderUserCatalog(container);
    });
  });

  container.querySelectorAll("[data-check-attr]").forEach((btn) => {
    btn.addEventListener("click", async (e) => {
      e.stopPropagation();
      const attrId = Number(btn.getAttribute("data-check-attr"));
      await openAttractionDetailsModal(attrId);
    });
  });

  container.querySelectorAll("[data-cat-filter]").forEach((chip) => {
    chip.addEventListener("click", () => {
      state.catalogFilter = chip.getAttribute("data-cat-filter");
      renderUserCatalog(container);
    });
  });

  container.querySelector("#catalogSearchInput").addEventListener("input", (e) => {
    state.searchQuery = e.target.value;
    renderUserCatalog(container);
  });

  container.querySelector("#catalogGroupSizeInput").addEventListener("change", (e) => {
    state.groupSizeFilter = e.target.value.trim();
    renderUserCatalog(container);
  });

  container.querySelector("#onlyCompatibleCheckbox").addEventListener("change", (e) => {
    if (e.target.checked && !state.profile) {
      showToast("Najpierw określ swój profil uczestnika / grupy, aby filtrować po kompatybilności.", "warning");
      state.userTab = "profile";
      renderApp();
      return;
    }
    state.onlyCompatibleOffers = e.target.checked;
    renderUserCatalog(container);
  });

  container.querySelectorAll("[data-offer-details]").forEach((btn) => {
    btn.addEventListener("click", () => openOfferDetailsModal(Number(btn.getAttribute("data-offer-details"))));
  });

  container.querySelectorAll("[data-offer-select]").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const offerId = Number(btn.getAttribute("data-offer-select"));
      const offerObj = visibleOffers.find((o) => o.id === offerId);
      await startPackageWithOffer(offerObj);
    });
  });
}

function renderOfferCardHtml(o) {
  const isIncompat = o.compatibility && !o.compatibility.isCompatible;
  const topFeatures = (o.effectiveFeatures || []).slice(0, 5);

  return `
    <article class="offer-card ${isIncompat ? "incompatible-card" : ""}">
      <div>
        <div class="offer-card-top">
          <div>
            <div class="offer-attraction-label">${esc(o.attractionName)}</div>
            <h3 class="offer-title">#${o.id} ${esc(o.name)}</h3>
          </div>
          <div class="offer-price-box">
            <div class="offer-price">${money(o.price)}</div>
            <div class="offer-price-unit">bilet normalny / os.</div>
          </div>
        </div>

        <div class="badges-row" style="margin-top:10px;">
          <span class="badge ${o.isToday ? "badge-today" : ""}">${esc(o.date)}${o.isToday ? " • DZISIAJ" : ""}</span>
          <span class="badge ${o.isExactGroupSizeOffer ? "badge-group-exact" : o.isGroupOffer ? "badge-group" : ""}">
            ${esc(o.groupSizeLabel)}
          </span>
          <span class="badge ${o.remainingSpots === 0 ? "badge-fail" : ""}">
            Wolne miejsca: ${o.remainingSpots}/${o.effectiveMaxParticipants}
          </span>
          ${
            o.compatibility
              ? `<span class="badge ${o.compatibility.isCompatible ? "badge-ok" : "badge-fail"}">
                  ${o.compatibility.isCompatible ? "Zgodna z profilem" : "Niezgodna z profilem"}
                 </span>`
              : ""
          }
        </div>

        <p class="offer-desc">${esc(o.description)}</p>

        <div class="feature-pills" style="margin-top:10px;">
          ${topFeatures
            .map(
              (f) => `<span class="feature-pill ${f.overridesAttractionDefault ? "override" : ""}" title="${esc(f.sourceElement)}">
                ${esc(f.featureName)}: ${esc(f.formattedValue)}
              </span>`
            )
            .join("")}
        </div>

        ${
          isIncompat
            ? `<div class="compat-box fail" style="margin-top:12px;padding:12px 15px;font-size:14.5px;">
                <strong>Powody niezgodności:</strong>
                <ul>${o.compatibility.incompatibilityReasons.map((r) => `<li>${esc(r)}</li>`).join("")}</ul>
               </div>`
            : ""
        }
      </div>

      <div style="border-top:1px solid var(--border);padding-top:12px;display:flex;justify-content:space-between;align-items:center;gap:8px;">
        <span class="form-hint">Dodatki: ${o.addonsCount} • Powiązane: ${o.relatedOffersCount}</span>
        <div class="btn-row">
          <button type="button" class="btn btn-sm" data-offer-details="${o.id}">Szczegóły</button>
          <button type="button" class="btn btn-sm btn-primary" data-offer-select="${o.id}">Wybierz pakiet</button>
        </div>
      </div>
    </article>
  `;
}

async function openAttractionDetailsModal(attrId) {
  const detail = await api.getAttraction(attrId);
  let compatHtml = `<p class="form-hint">Skonfiguruj profil uczestnika w zakładce „Profil uczestnika / grupy”, aby automatycznie zweryfikować dostępność tego obiektu.</p>`;

  if (state.profile) {
    const report = await api.validateAttraction(attrId, state.profile);
    compatHtml = renderCompatibilityBox(report, false);
  }

  const bodyHtml = `
    <div>
      <div style="margin-bottom:12px;">
        <div><strong>Lokalizacja:</strong> ${esc(detail.location)}</div>
        <div><strong>Maksymalna liczba osób na atrakcji:</strong> <span class="badge badge-group">${detail.maxParticipants} os. jednocześnie</span></div>
        <p style="margin:8px 0;">${esc(detail.description)}</p>
      </div>

      <h4 style="margin:12px 0 6px;">Cechy ogólne obiektu (domyślne dla ofert)</h4>
      <div class="feature-pills" style="margin-bottom:14px;">
        ${
          detail.features.length
            ? detail.features.map((f) => `<span class="feature-pill">${esc(f.featureName)}: <strong>${esc(f.formattedValue)}</strong></span>`).join("")
            : `<span class="form-hint">Brak zdefiniowanych cech ogólnych</span>`
        }
      </div>

      <h4 style="margin:12px 0 8px;">Weryfikacja ogólnej dostępności obiektu</h4>
      ${compatHtml}
    </div>
  `;

  openModal(`Atrakcja #${detail.id}: ${detail.name}`, bodyHtml);
}

async function openOfferDetailsModal(offerId) {
  const detail = await api.getOfferDetails(offerId, state.profile, true);
  const o = detail.offer;

  const bodyHtml = `
    <div>
      <div class="badges-row" style="margin-bottom:10px;">
        <span class="badge">${esc(o.attractionName)}</span>
        <span class="badge">${esc(o.date)} (${esc(o.statusLabel)})</span>
        <span class="badge badge-group">${esc(o.groupSizeLabel)}</span>
        <span class="badge">Wolne miejsca: ${o.remainingSpots}/${o.effectiveMaxParticipants} os.</span>
      </div>

      <p style="margin-top:0;">${esc(o.description)}</p>
      <div style="margin-bottom:12px;">
        <strong>Cena bazowa:</strong> ${money(o.price)} / os. •
        <span class="form-hint">Zniżki: Dzieci -50%, Młodzież/Student -30%, Senior -25%, OzN -50% + rabat grupowy (4-7 os.: -5%, 8+ os.: -10%)</span>
      </div>

      ${o.compatibility ? renderCompatibilityBox(o.compatibility, false) : ""}

      <h4 style="margin:14px 0 8px;">Efektywne cechy i wymagania trasy (${o.effectiveFeatures.length})</h4>
      <div class="table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th>Cecha</th>
              <th>Wartość</th>
              <th>Źródło pochodzenia</th>
            </tr>
          </thead>
          <tbody>
            ${o.effectiveFeatures
              .map(
                (f) => `
              <tr>
                <td><strong>${esc(f.featureName)}</strong></td>
                <td>${esc(f.formattedValue)}</td>
                <td>
                  ${
                    f.isDirectOfferFeature
                      ? `<span class="badge ${f.overridesAttractionDefault ? "badge-warn" : "badge-group"}">
                          ${f.overridesAttractionDefault ? "Cecha oferty (nadpisuje atrakcję)" : "Cecha własna oferty"}
                         </span>`
                      : `<span class="form-hint">Dziedziczone z atrakcji</span>`
                  }
                </td>
              </tr>
            `
              )
              .join("")}
          </tbody>
        </table>
      </div>

      <h4 style="margin:16px 0 8px;">Dostępne dodatki (${detail.addons.length})</h4>
      ${
        detail.addons.length === 0
          ? `<p class="form-hint">Brak dodatków dla tej oferty.</p>`
          : detail.addons
              .map(
                (a) => `
            <div class="selectable-item">
              <div>
                <strong>${esc(a.name)}</strong> (+${money(a.price)})
                <div class="form-hint">${esc(a.description)}</div>
                ${
                  a.featureModifiers?.length
                    ? `<div class="badges-row" style="margin-top:4px;">
                        ${a.featureModifiers
                          .map(
                            (m) => `<span class="badge ${m.effectMode === "ProvideAccommodation" ? "badge-ok" : "badge-warn"}">
                              ${m.effectMode === "ProvideAccommodation" ? "Udogodnienie" : "Wymaganie"}: ${esc(m.featureName)} = ${esc(m.formattedValue)}
                            </span>`
                          )
                          .join("")}
                       </div>`
                    : ""
                }
              </div>
            </div>
          `
              )
              .join("")
      }

      <h4 style="margin:16px 0 8px;">Polecane oferty powiązane (${detail.relatedOffers.length})</h4>
      ${
        detail.relatedOffers.length === 0
          ? `<p class="form-hint">Brak dostępnych ofert powiązanych.</p>`
          : `<div class="badges-row">
              ${detail.relatedOffers
                .map((r) => `<span class="badge">#${r.id} ${esc(r.name)} (${money(r.price)} | ${esc(r.groupSizeLabel)})</span>`)
                .join("")}
             </div>`
      }
    </div>
  `;

  const footerHtml = `
    <button type="button" class="btn" id="closeDetailModalBtn">Zamknij</button>
    <button type="button" class="btn btn-primary" id="startBuilderFromModalBtn">Skonfiguruj pakiet i kup</button>
  `;

  const { footerEl } = openModal(`Szczegóły oferty #${o.id}`, bodyHtml, footerHtml);
  footerEl.querySelector("#closeDetailModalBtn").addEventListener("click", closeModal);
  footerEl.querySelector("#startBuilderFromModalBtn").addEventListener("click", async () => {
    closeModal();
    await startPackageWithOffer(o);
  });
}

async function startPackageWithOffer(offerSummary) {
  if (!offerSummary) return;

  if (offerSummary.compatibility && !offerSummary.compatibility.isCompatible) {
    const proceed = confirm(
      `Uwaga: Oferta „${offerSummary.name}” ma obecnie niespełnione wymagania względem Twojego profilu:\n\n` +
        offerSummary.compatibility.incompatibilityReasons.map((r) => `• ${r}`).join("\n") +
        `\n\nCzy mimo to chcesz rozpocząć konfigurację (np. aby dobrać dodatek zapewniający udogodnienie jak winda lub meleks)?`
    );
    if (!proceed) return;
  }

  state.setPackageConfig({
    baseOfferId: offerSummary.id,
    selectedAddonIds: [],
    relatedOffers: []
  });
  state.customGroupBreakdown = null;
  await refreshPackageEvaluation();
  state.userTab = "builder";
  await renderApp();
}

// ============================================================================
// 2. KREATOR PAKIETU, DODATKÓW, OFERT POWIĄZANYCH I ZNIŻEK GRUPOWYCH
// ============================================================================

async function renderPackageBuilder(container) {
  if (!state.packageConfig?.baseOfferId || !state.packageEvaluation) {
    container.innerHTML = `
      <div class="panel" style="text-align:center;padding:38px 20px;">
        <h2 style="margin-top:0;">Nie wybrano jeszcze oferty głównej do pakietu</h2>
        <p class="section-desc" style="margin-bottom:18px;">Przejdź do katalogu ofert, wybierz interesującą trasę (indywidualną lub grupową, np. 4-osobową), a następnie dobierz dodatki, powiązane oferty i zniżki biletowe.</p>
        <button type="button" class="btn btn-primary" id="goToCatalogBtn">Przejdź do katalogu ofert</button>
      </div>
    `;
    container.querySelector("#goToCatalogBtn").addEventListener("click", () => {
      state.userTab = "catalog";
      renderApp();
    });
    return;
  }

  const ev = state.packageEvaluation;
  const baseDetail = await api.getOfferDetails(ev.baseOfferId, state.profile, true);
  const selectedBaseAddonIds = new Set(state.packageConfig.selectedAddonIds);
  const selectedRelatedMap = new Map(
    state.packageConfig.relatedOffers.map((r) => [r.offerId, new Set(r.selectedAddonIds || [])])
  );

  const activeBreakdown = state.customGroupBreakdown || ev.suggestedGroupBreakdown;
  const pb = ev.priceBreakdown;

  const isOrderBlockedByProfile = ev.compatibility && !ev.compatibility.isCompatible;
  const isOrderBlockedBySpots = ev.hasGroupBoundsConflict || ev.minRemainingSpots < ev.minParticipants;

  container.innerHTML = `
    <div class="section-header">
      <div>
        <h2 class="section-title">Konfigurator pakietu, zniżek grupowych i zamówienia</h2>
        <p class="section-desc">Dobierz proponowane dodatki i oferty powiązane, podziel grupę na ulgi biletowe i sfinalizuj rezerwację.</p>
      </div>
      <div class="btn-row">
        <button type="button" class="btn" id="changeBaseOfferBtn">Zmień ofertę główną</button>
        <button type="button" class="btn btn-danger" id="clearPackageBtn">Wyczyść koszyk</button>
      </div>
    </div>

    <div class="builder-layout">
      <div>
        <!-- Krok 1 & 2: Oferta główna -->
        <div class="panel">
          <div style="display:flex;justify-content:space-between;align-items:flex-start;gap:12px;">
            <div>
              <span class="badge badge-group">Krok 1–2 • Oferta główna</span>
              <h3 style="margin:6px 0 4px;">#${ev.baseOfferId} ${esc(ev.baseOfferName)}</h3>
              <div class="form-hint">${esc(ev.attractionName)} • Data realizacji: ${esc(ev.date)} • Wymagana grupa: ${esc(ev.groupSizeLabel)}</div>
            </div>
            <div style="text-align:right;">
              <div class="offer-price">${money(ev.basePrice)}</div>
              <div class="offer-price-unit">cena bazowa / os.</div>
            </div>
          </div>
        </div>

        <!-- Krok 3: Dodatki do oferty głównej -->
        <div class="panel">
          <h3 class="panel-title">Krok 3/5 • Proponowane dodatki do oferty głównej (${baseDetail.addons.length})</h3>
          ${
            baseDetail.addons.length === 0
              ? `<p class="form-hint">Oferta główna nie posiada dodatkowych opcji płatnych.</p>`
              : baseDetail.addons
                  .map((addon) => {
                    const checked = selectedBaseAddonIds.has(addon.id);
                    return `
                  <div class="selectable-item ${checked ? "selected" : ""}">
                    <div>
                      <label class="checkbox-line" style="font-weight:700;">
                        <input type="checkbox" data-toggle-base-addon="${addon.id}" ${checked ? "checked" : ""} />
                        <span>${esc(addon.name)} (+${money(addon.price)})</span>
                      </label>
                      <div class="form-hint" style="margin-left:24px;">${esc(addon.description)}</div>
                      ${
                        addon.featureModifiers?.length
                          ? `<div class="badges-row" style="margin-left:24px;margin-top:6px;">
                              ${addon.featureModifiers
                                .map(
                                  (m) => `<span class="badge ${m.effectMode === "ProvideAccommodation" ? "badge-ok" : "badge-warn"}">
                                    ${m.effectMode === "ProvideAccommodation" ? "Udogodnienie" : "Wymaganie"}: ${esc(m.featureName)} = ${esc(m.formattedValue)}
                                  </span>`
                                )
                                .join("")}
                             </div>`
                          : ""
                      }
                    </div>
                  </div>
                `;
                  })
                  .join("")
          }
        </div>

        <!-- Krok 4: Oferty powiązane -->
        <div class="panel">
          <h3 class="panel-title">Krok 4/5 • Polecane oferty powiązane N:M (${baseDetail.relatedOffers.length})</h3>
          ${
            baseDetail.relatedOffers.length === 0
              ? `<p class="form-hint">Brak dostępnych ofert powiązanych dla tej trasy.</p>`
              : baseDetail.relatedOffers
                  .map((rel) => {
                    const isSelected = selectedRelatedMap.has(rel.id);
                    const relSelectedView = ev.selectedRelatedOffers.find((x) => x.offer.id === rel.id);
                    const relAddonSet = selectedRelatedMap.get(rel.id) || new Set();

                    return `
                  <div class="selectable-item ${isSelected ? "selected" : ""}" style="flex-direction:column;align-items:stretch;">
                    <div style="display:flex;justify-content:space-between;align-items:flex-start;gap:12px;">
                      <div>
                        <label class="checkbox-line" style="font-weight:700;">
                          <input type="checkbox" data-toggle-related="${rel.id}" ${isSelected ? "checked" : ""} />
                          <span>#${rel.id} ${esc(rel.name)} (+${money(rel.price)})</span>
                        </label>
                        <div class="form-hint" style="margin-left:24px;">
                          ${esc(rel.attractionName)} • ${esc(rel.date)} • ${esc(rel.groupSizeLabel)}
                          ${
                            rel.compatibility
                              ? ` • <strong style="color:${rel.compatibility.isCompatible ? "var(--success)" : "var(--danger)"};">
                                  ${rel.compatibility.isCompatible ? "Zgodna z profilem" : "Niezgodna z profilem (!)"}
                                 </strong>`
                              : ""
                          }
                        </div>
                      </div>
                    </div>

                    ${
                      isSelected && relSelectedView?.availableAddons?.length
                        ? `<div style="margin-left:28px;margin-top:12px;padding-top:10px;border-top:1.5px dashed var(--border-strong);">
                            <div style="font-size:15px;font-weight:700;margin-bottom:8px;color:var(--blue-deep);">Dodatki do oferty powiązanej:</div>
                            ${relSelectedView.availableAddons
                              .map(
                                (ra) => `
                              <label class="checkbox-line" style="margin-bottom:6px;font-size:15px;">
                                <input type="checkbox" data-toggle-rel-addon="${ra.id}" data-rel-parent="${rel.id}" ${relAddonSet.has(ra.id) ? "checked" : ""} />
                                <span>${esc(ra.name)} (+${money(ra.price)})</span>
                              </label>
                            `
                              )
                              .join("")}
                           </div>`
                        : ""
                    }
                  </div>
                `;
                  })
                  .join("")
          }
        </div>

        <!-- Zagregowane wymagania całego pakietu i raport kompatybilności -->
        <div class="panel">
          <h3 class="panel-title">Zagregowane cechy i weryfikacja całego pakietu</h3>
          <div style="margin-bottom:12px;">
            <span class="badge badge-group">Wymagana wielkość grupy pakietu: ${esc(ev.groupSizeLabel)}</span>
            <span class="badge ${ev.minRemainingSpots < ev.minParticipants ? "badge-fail" : ""}">
              Dostępna pula miejsc: ${ev.minRemainingSpots} os. (ogranicza: ${esc(ev.limitingSpotsSource)})
            </span>
          </div>

          ${
            ev.compatibility
              ? renderCompatibilityBox(ev.compatibility, false)
              : `<div class="form-hint" style="margin-bottom:12px;">Nie ustawiono profilu uczestnika – przejdź do zakładki „Profil uczestnika / grupy”, aby sprawdzić indywidualną kompatybilność wiekową i dostępnościową.</div>`
          }

          <div class="table-wrap" style="margin-top:12px;">
            <table class="data-table">
              <thead>
                <tr>
                  <th>Zagregowana cecha pakietu</th>
                  <th>Wartość obowiązująca</th>
                  <th>Element narzucający wymóg</th>
                </tr>
              </thead>
              <tbody>
                ${ev.packageEffectiveFeatures
                  .map(
                    (pf) => `
                  <tr>
                    <td><strong>${esc(pf.featureName)}</strong></td>
                    <td>${esc(pf.formattedValue)}</td>
                    <td class="form-hint">${esc(pf.sourceElement)}</td>
                  </tr>
                `
                  )
                  .join("")}
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- Prawa kolumna: Podział grupy na zniżki i finalizacja zamówienia -->
      <div class="sticky-sidebar">
        <div class="panel">
          <h3 class="panel-title">Krok 5/5 • Podział grupy i zniżki</h3>
          <div class="form-hint" style="margin-bottom:10px;">
            Rozdziel uczestników na kategorie ulgowe. Przy 4–7 os. naliczany jest dodatkowy rabat grupowy <strong>-5%</strong>, a od 8 os. <strong>-10%</strong>.
          </div>

          ${
            state.profile
              ? `<button type="button" class="btn btn-sm" id="syncGroupFromProfileBtn" style="margin-bottom:10px;width:100%;">
                  Wczytaj podział kategorii z mojego profilu
                 </button>`
              : ""
          }

          ${renderCategoryCounterRow("adultsCount", "Dorosły (normalny)", "0%", activeBreakdown.adultsCount)}
          ${renderCategoryCounterRow("childrenCount", "Dziecko (do 12 lat)", "-50%", activeBreakdown.childrenCount)}
          ${renderCategoryCounterRow("youthOrStudentsCount", "Młodzież / Student (13–26)", "-30%", activeBreakdown.youthOrStudentsCount)}
          ${renderCategoryCounterRow("seniorsCount", "Senior (65+ lat)", "-25%", activeBreakdown.seniorsCount)}
          ${renderCategoryCounterRow("disabledCount", "Osoba z niepełnosprawnością", "-50%", activeBreakdown.disabledCount)}

          <div class="price-summary-lines" style="margin-top:14px;">
            <div class="price-line">
              <span>Cena pakietu (1 bilet normalny):</span>
              <strong>${money(ev.totalPricePerPerson)}</strong>
            </div>
            <div class="price-line">
              <span>Łącznie uczestników w grupie:</span>
              <strong>${pb.totalParticipants} os. (wymagane: ${esc(ev.groupSizeLabel)})</strong>
            </div>
            <div class="price-line">
              <span>Wartość bez zniżek:</span>
              <span>${money(pb.regularGrossTotal)}</span>
            </div>
            ${
              pb.categoryDiscountsSavings > 0
                ? `<div class="price-line savings">
                    <span>Zniżki ulgowe (dzieci/studenci/seniorzy/OzN):</span>
                    <span>-${money(pb.categoryDiscountsSavings)}</span>
                   </div>`
                : ""
            }
            ${
              pb.extraGroupDiscountAmount > 0
                ? `<div class="price-line savings">
                    <span>Dodatkowy rabat grupowy (-${pb.extraGroupDiscountPercent}%):</span>
                    <span>-${money(pb.extraGroupDiscountAmount)}</span>
                   </div>`
                : ""
            }
            <div class="price-line total">
              <span>Do zapłaty:</span>
              <span>${money(pb.finalTotalPrice)}</span>
            </div>
          </div>

          <form id="checkoutForm" style="margin-top:16px;padding-top:14px;border-top:1px solid var(--border);">
            <div class="form-group">
              <label>Imię i nazwisko zamawiającego / opiekuna grupy *</label>
              <input type="text" id="custNameInput" required placeholder="np. Jan Kowalski" />
            </div>
            <div class="form-group">
              <label>Adres e-mail do potwierdzenia (opcjonalnie)</label>
              <input type="email" id="custEmailInput" placeholder="jan.kowalski@domena.pl" />
            </div>
            <div class="form-group">
              <label>Sposób płatności</label>
              <select id="paymentMethodSelect">
                <option value="Blik">BLIK</option>
                <option value="Card">Karta płatnicza</option>
                <option value="OnlineTransfer">Szybki przelew online</option>
                <option value="CashAtTicketOffice">Płatność w kasie na miejscu</option>
              </select>
            </div>

            ${
              isOrderBlockedByProfile
                ? `<div class="compat-box fail" style="margin-bottom:12px;font-size:14.5px;">
                    Finalizacja zablokowana: skonfigurowany pakiet nie spełnia wymagań aktywnego profilu uczestnika.
                   </div>`
                : ""
            }
            ${
              isOrderBlockedBySpots
                ? `<div class="compat-box fail" style="margin-bottom:12px;font-size:14.5px;">
                    Brak wystarczającej liczby wolnych miejsc lub sprzeczność limitów grupowych w pakiecie.
                   </div>`
                : ""
            }

            <button type="submit" class="btn btn-primary" style="width:100%;padding:14px;font-size:17px;" ${isOrderBlockedByProfile || isOrderBlockedBySpots ? "disabled" : ""}>
              Złóż zamówienie (${money(pb.finalTotalPrice)})
            </button>
          </form>
        </div>
      </div>
    </div>
  `;

  container.querySelector("#changeBaseOfferBtn").addEventListener("click", () => {
    state.userTab = "catalog";
    renderApp();
  });

  container.querySelector("#clearPackageBtn").addEventListener("click", async () => {
    state.setPackageConfig(null);
    state.customGroupBreakdown = null;
    await refreshPackageEvaluation();
    await renderApp();
  });

  // Obsługa zaznaczania dodatków głównych z ostrzeżeniem kompatybilności
  container.querySelectorAll("[data-toggle-base-addon]").forEach((cb) => {
    cb.addEventListener("change", async (e) => {
      const addonId = Number(cb.getAttribute("data-toggle-base-addon"));
      const adding = e.target.checked;

      if (adding && state.profile) {
        const preview = await api.previewPackageChange({
          configuration: state.packageConfig,
          profile: state.profile,
          changeType: "baseAddon",
          targetAddonId: addonId
        });

        if (preview.newIssues?.length > 0) {
          const allow = confirm(
            `OSTRZEŻENIE KOMPATYBILNOŚCI:\nWybrany dodatek powoduje niezgodność pakietu z Twoim profilem:\n\n` +
              preview.newIssues.map((i) => `• ${i.formattedMessage}`).join("\n") +
              `\n\nCzy mimo to chcesz dodać go do roboczej konfiguracji (finalizacja zakupu zostanie zablokowana)?`
          );
          if (!allow) {
            e.target.checked = false;
            return;
          }
        } else if (preview.resolvedPreviousIssues) {
          showToast("Wybrany dodatek zapewnia udogodnienie i dopasował ofertę do Twojego profilu!", "success");
        }
      }

      const nextAddons = new Set(state.packageConfig.selectedAddonIds);
      if (adding) nextAddons.add(addonId);
      else nextAddons.delete(addonId);

      state.setPackageConfig({
        ...state.packageConfig,
        selectedAddonIds: Array.from(nextAddons)
      });
      await refreshPackageEvaluation();
      await renderPackageBuilder(container);
    });
  });

  // Obsługa ofert powiązanych z ostrzeżeniem kompatybilności
  container.querySelectorAll("[data-toggle-related]").forEach((cb) => {
    cb.addEventListener("change", async (e) => {
      const relOfferId = Number(cb.getAttribute("data-toggle-related"));
      const adding = e.target.checked;

      if (adding && state.profile) {
        const preview = await api.previewPackageChange({
          configuration: state.packageConfig,
          profile: state.profile,
          changeType: "relatedOffer",
          targetOfferId: relOfferId
        });

        if (preview.newIssues?.length > 0) {
          const allow = confirm(
            `OSTRZEŻENIE KOMPATYBILNOŚCI:\nDołączenie tej oferty powiązanej powoduje niezgodność pakietu z Twoim profilem:\n\n` +
              preview.newIssues.map((i) => `• ${i.formattedMessage}`).join("\n") +
              `\n\nCzy mimo to dołączyć ofertę powiązaną?`
          );
          if (!allow) {
            e.target.checked = false;
            return;
          }
        }
      }

      let nextRelated = [...state.packageConfig.relatedOffers];
      if (adding) {
        nextRelated.push({ offerId: relOfferId, selectedAddonIds: [] });
      } else {
        nextRelated = nextRelated.filter((r) => r.offerId !== relOfferId);
      }

      state.setPackageConfig({
        ...state.packageConfig,
        relatedOffers: nextRelated
      });
      await refreshPackageEvaluation();
      await renderPackageBuilder(container);
    });
  });

  // Dodatki ofert powiązanych
  container.querySelectorAll("[data-toggle-rel-addon]").forEach((cb) => {
    cb.addEventListener("change", async (e) => {
      const addonId = Number(cb.getAttribute("data-toggle-rel-addon"));
      const parentId = Number(cb.getAttribute("data-rel-parent"));
      const adding = e.target.checked;

      if (adding && state.profile) {
        const preview = await api.previewPackageChange({
          configuration: state.packageConfig,
          profile: state.profile,
          changeType: "relatedAddon",
          targetOfferId: parentId,
          targetAddonId: addonId
        });

        if (preview.newIssues?.length > 0) {
          const allow = confirm(
            `OSTRZEŻENIE:\nDodatek oferty powiązanej narusza wymagania Twojego profilu:\n\n` +
              preview.newIssues.map((i) => `• ${i.formattedMessage}`).join("\n") +
              `\n\nCzy mimo to go zaznaczyć?`
          );
          if (!allow) {
            e.target.checked = false;
            return;
          }
        }
      }

      const nextRelated = state.packageConfig.relatedOffers.map((r) => {
        if (r.offerId !== parentId) return r;
        const set = new Set(r.selectedAddonIds || []);
        if (adding) set.add(addonId);
        else set.delete(addonId);
        return { ...r, selectedAddonIds: Array.from(set) };
      });

      state.setPackageConfig({
        ...state.packageConfig,
        relatedOffers: nextRelated
      });
      await refreshPackageEvaluation();
      await renderPackageBuilder(container);
    });
  });

  // Liczniki kategorii biletowych i zniżek
  const syncBtn = container.querySelector("#syncGroupFromProfileBtn");
  if (syncBtn) {
    syncBtn.addEventListener("click", async () => {
      state.customGroupBreakdown = null;
      await refreshPackageEvaluation();
      await renderPackageBuilder(container);
    });
  }

  container.querySelectorAll("[data-cat-delta]").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const field = btn.getAttribute("data-cat-field");
      const delta = Number(btn.getAttribute("data-cat-delta"));
      const next = { ...activeBreakdown };
      next[field] = Math.max(0, (next[field] || 0) + delta);
      const total =
        next.adultsCount +
        next.childrenCount +
        next.youthOrStudentsCount +
        next.seniorsCount +
        next.disabledCount;
      if (total < 1) return;

      state.customGroupBreakdown = next;
      await refreshPackageEvaluation();
      await renderPackageBuilder(container);
    });
  });

  // Finalizacja zamówienia
  container.querySelector("#checkoutForm").addEventListener("submit", async (e) => {
    e.preventDefault();
    const customerName = container.querySelector("#custNameInput").value.trim();
    const customerEmail = container.querySelector("#custEmailInput").value.trim();
    const paymentMethod = container.querySelector("#paymentMethodSelect").value;

    try {
      const createdOrder = await api.placeOrder({
        configuration: state.packageConfig,
        customerName,
        customerEmail,
        paymentMethod,
        groupBreakdown: activeBreakdown,
        profile: state.profile
      });

      state.setPackageConfig(null);
      state.customGroupBreakdown = null;
      await refreshPackageEvaluation();

      showToast(`Złożono zamówienie ${createdOrder.orderNumber}!`, "success");
      openModal(`Potwierdzenie zamówienia ${createdOrder.orderNumber}`, renderOrderReceiptHtml(createdOrder));
      state.userTab = "orders";
      await renderApp();
    } catch (err) {
      showToast(err.message, "error");
    }
  });
}

function renderCategoryCounterRow(field, label, discountText, count) {
  return `
    <div class="counter-row">
      <div>
        <div style="font-weight:700;font-size:15.5px;">${esc(label)}</div>
        <div class="form-hint">Ulga kategorii: <strong>${esc(discountText)}</strong></div>
      </div>
      <div class="counter-controls">
        <button type="button" class="counter-btn" data-cat-field="${field}" data-cat-delta="-1">&minus;</button>
        <span class="counter-val">${count || 0}</span>
        <button type="button" class="counter-btn" data-cat-field="${field}" data-cat-delta="1">+</button>
      </div>
    </div>
  `;
}

// ============================================================================
// 3. PROFIL UCZESTNIKA / GRUPY (DYNAMICZNIE ZINTEGROWANY Z FEATURES.JSON)
// ============================================================================

async function renderProfileTab(container) {
  await refreshGlobalData();
  const p = state.profile || {
    participantAges: [35, 10],
    minParticipantHeightCm: null,
    requiresWheelchairAccess: false,
    disabledParticipantsCount: 0,
    maxPhysicalDemand: "Średni",
    explicitAdultGuardianPresent: null,
    requiresChildFriendly: true,
    requiresStrollerAccess: false,
    requiresElevator: false,
    requiredFacilityFeatureIds: [],
    confirmedConditionFeatureIds: [],
    customNumericValues: {},
    customOptionOrTextValues: {}
  };

  const physicalFeat = state.features.find((f) => f.profileBindingKey === "physical_demand");
  const physicalOptions = physicalFeat?.allowedValues?.length
    ? physicalFeat.allowedValues
    : ["Niski", "Średni", "Wysoki"];

  // Wszystkie dynamiczne cechy dodane w Features.json bez wbudowanego klucza
  const dynamicFeatures = state.features.filter((f) => !f.profileBindingKey);

  container.innerHTML = `
    <div class="section-header">
      <div>
        <h2 class="section-title">Profil uczestnika lub grupy uczestników</h2>
        <p class="section-desc">Określ wiek uczestników (pojedynczej osoby lub grupy), potrzeby dostępności oraz parametry dla dowolnych cech ze słownika Features.json.</p>
      </div>
      ${
        state.profile
          ? `<button type="button" class="btn btn-danger" id="clearProfileBtn">Wyczyść aktywny profil</button>`
          : ""
      }
    </div>

    <form id="profileForm" class="panel">
      <h3 class="panel-title">Dane podstawowe i wielkość grupy</h3>
      <div class="form-grid">
        <div class="form-group">
          <label>Wiek uczestnika lub uczestników grupy (po przecinku) *</label>
          <input type="text" id="profAgesInput" required value="${esc((p.participantAges || []).join(", "))}" placeholder="np. 28 lub dla grupy 4 os.: 38, 68, 20, 10" />
          <span class="form-hint">Na podstawie wieku system weryfikuje progi wiekowe oraz automatycznie wylicza zniżki (Dzieci -50%, Młodzież/Studenci -30%, Seniorzy -25%).</span>
        </div>

        <div class="form-group">
          <label>Wzrost najniższego uczestnika w cm (opcjonalnie)</label>
          <input type="number" min="40" max="260" id="profHeightInput" value="${p.minParticipantHeightCm ?? ""}" placeholder="np. 135" />
        </div>

        <div class="form-group">
          <label>Maksymalny akceptowalny wysiłek fizyczny</label>
          <select id="profPhysicalSelect">
            <option value="">Bez ograniczeń</option>
            ${physicalOptions
              .map(
                (opt) => `<option value="${esc(opt)}" ${p.maxPhysicalDemand === opt ? "selected" : ""}>${esc(opt)}</option>`
              )
              .join("")}
          </select>
        </div>

        <div class="form-group">
          <label>Liczba osób z orzeczeniem o niepełnosprawności (ulga -50%)</label>
          <input type="number" min="0" max="100" id="profDisabledCountInput" value="${p.disabledParticipantsCount ?? 0}" />
        </div>
      </div>

      <h3 class="panel-title" style="margin-top:12px;">Wymagane udogodnienia i dostępność</h3>
      <div class="form-grid">
        <label class="checkbox-line">
          <input type="checkbox" id="profWheelchairCb" ${p.requiresWheelchairAccess ? "checked" : ""} />
          <span>Wymagany dostęp dla wózka inwalidzkiego</span>
        </label>
        <label class="checkbox-line">
          <input type="checkbox" id="profChildFriendlyCb" ${p.requiresChildFriendly ? "checked" : ""} />
          <span>Wymagane oferty przyjazne dla dzieci</span>
        </label>
        <label class="checkbox-line">
          <input type="checkbox" id="profStrollerCb" ${p.requiresStrollerAccess ? "checked" : ""} />
          <span>Możliwość wejścia z wózkiem dziecięcym</span>
        </label>
        <label class="checkbox-line">
          <input type="checkbox" id="profElevatorCb" ${p.requiresElevator ? "checked" : ""} />
          <span>Wymagana dostępność windy</span>
        </label>
        <label class="checkbox-line">
          <input type="checkbox" id="profGuardianCb" ${p.explicitAdultGuardianPresent === true ? "checked" : ""} />
          <span>Osobom niepełnoletnim towarzyszy dorosły opiekun (gdy nie wpisano wieku dorosłego)</span>
        </label>
      </div>

      ${
        dynamicFeatures.length > 0
          ? `
        <div style="border-top:1px solid var(--border);margin-top:18px;padding-top:16px;">
          <h3 class="panel-title">Dodatkowe kryteria ze słownika Features.json (${dynamicFeatures.length})</h3>
          <div class="form-grid">
            ${dynamicFeatures
              .map((df) => {
                if (df.comparisonRule === "FacilityAvailable") {
                  const checked = (p.requiredFacilityFeatureIds || []).includes(df.id);
                  return `
                    <label class="checkbox-line">
                      <input type="checkbox" data-dyn-facility="${df.id}" ${checked ? "checked" : ""} />
                      <span>Wymagam: <strong>${esc(df.name)}</strong></span>
                    </label>`;
                }
                if (df.comparisonRule === "ConditionRequired") {
                  const checked = (p.confirmedConditionFeatureIds || []).includes(df.id);
                  return `
                    <label class="checkbox-line">
                      <input type="checkbox" data-dyn-condition="${df.id}" ${checked ? "checked" : ""} />
                      <span>Spełniam warunek: <strong>${esc(df.name)}</strong></span>
                    </label>`;
                }
                if (df.type === "Number") {
                  const curNums = (p.customNumericValues && p.customNumericValues[df.id]) || [];
                  return `
                    <div class="form-group">
                      <label>${esc(df.name)} ${df.unit ? `(${esc(df.unit)})` : ""}</label>
                      <input type="text" data-dyn-number="${df.id}" value="${esc(curNums.join(", "))}" placeholder="Wartość lub wartości grupy po przecinku" />
                    </div>`;
                }
                if (df.type === "OptionList" && df.allowedValues?.length) {
                  const curOpt = (p.customOptionOrTextValues && p.customOptionOrTextValues[df.id]) || "";
                  return `
                    <div class="form-group">
                      <label>${esc(df.name)}</label>
                      <select data-dyn-text="${df.id}">
                        <option value="">Dowolna / pomiń</option>
                        ${df.allowedValues
                          .map((opt) => `<option value="${esc(opt)}" ${curOpt === opt ? "selected" : ""}>${esc(opt)}</option>`)
                          .join("")}
                      </select>
                    </div>`;
                }
                const curTxt = (p.customOptionOrTextValues && p.customOptionOrTextValues[df.id]) || "";
                return `
                  <div class="form-group">
                    <label>${esc(df.name)}</label>
                    <input type="text" data-dyn-text="${df.id}" value="${esc(curTxt)}" placeholder="np. Angielski (puste = dowolny)" />
                  </div>`;
              })
              .join("")}
          </div>
        </div>
      `
          : ""
      }

      <div class="btn-row" style="margin-top:18px;">
        <button type="submit" class="btn btn-primary">Zapisz profil i filtruj oferty</button>
        <button type="button" class="btn" id="applyPresetFamilyBtn">Szablon: Rodzina 4 os. (38, 68, 20, 10 lat)</button>
        <button type="button" class="btn" id="applyPresetWheelchairBtn">Szablon: Rodzina z wózkiem inwalidzkim (35, 8 lat)</button>
      </div>
    </form>
  `;

  const clearBtn = container.querySelector("#clearProfileBtn");
  if (clearBtn) {
    clearBtn.addEventListener("click", async () => {
      state.setProfile(null);
      state.onlyCompatibleOffers = false;
      await refreshPackageEvaluation();
      showToast("Wyczyszczono aktywny profil uczestnika.", "info");
      await renderProfileTab(container);
    });
  }

  container.querySelector("#applyPresetFamilyBtn").addEventListener("click", async () => {
    state.setProfile({
      participantAges: [38, 68, 20, 10],
      minParticipantHeightCm: 138,
      requiresWheelchairAccess: false,
      disabledParticipantsCount: 0,
      maxPhysicalDemand: "Średni",
      explicitAdultGuardianPresent: true,
      requiresChildFriendly: true,
      requiresStrollerAccess: false,
      requiresElevator: false,
      requiredFacilityFeatureIds: [],
      confirmedConditionFeatureIds: [],
      customNumericValues: {},
      customOptionOrTextValues: {}
    });
    state.customGroupBreakdown = null;
    await refreshPackageEvaluation();
    showToast("Załadowano profil 4-osobowej grupy wielopokoleniowej (zniżki + oferty 4 os.).", "success");
    state.userTab = "catalog";
    await renderApp();
  });

  container.querySelector("#applyPresetWheelchairBtn").addEventListener("click", async () => {
    state.setProfile({
      participantAges: [35, 8],
      minParticipantHeightCm: 128,
      requiresWheelchairAccess: true,
      disabledParticipantsCount: 1,
      maxPhysicalDemand: "Niski",
      explicitAdultGuardianPresent: true,
      requiresChildFriendly: true,
      requiresStrollerAccess: false,
      requiresElevator: false,
      requiredFacilityFeatureIds: [],
      confirmedConditionFeatureIds: [],
      customNumericValues: {},
      customOptionOrTextValues: {}
    });
    state.customGroupBreakdown = null;
    await refreshPackageEvaluation();
    showToast("Załadowano profil z wymogiem dostępności dla wózka inwalidzkiego i ulgi OzN.", "success");
    state.userTab = "catalog";
    await renderApp();
  });

  container.querySelector("#profileForm").addEventListener("submit", async (e) => {
    e.preventDefault();
    const rawAges = container.querySelector("#profAgesInput").value;
    const ages = rawAges
      .split(/[,;\s]+/)
      .map((t) => t.trim())
      .filter(Boolean)
      .map(Number)
      .filter((n) => Number.isInteger(n) && n >= 0 && n <= 120);

    if (ages.length === 0) {
      showToast("Podaj poprawny wiek przynajmniej jednej osoby (0–120).", "error");
      return;
    }

    const heightRaw = container.querySelector("#profHeightInput").value.trim();
    const reqFacilities = [];
    container.querySelectorAll("[data-dyn-facility]:checked").forEach((cb) => {
      reqFacilities.push(Number(cb.getAttribute("data-dyn-facility")));
    });

    const confConditions = [];
    container.querySelectorAll("[data-dyn-condition]:checked").forEach((cb) => {
      confConditions.push(Number(cb.getAttribute("data-dyn-condition")));
    });

    const customNums = {};
    container.querySelectorAll("[data-dyn-number]").forEach((inp) => {
      const fid = Number(inp.getAttribute("data-dyn-number"));
      const nums = inp.value
        .split(/[,;]+/)
        .map((s) => Number(s.trim().replace(",", ".")))
        .filter((n) => !Number.isNaN(n) && inp.value.trim() !== "");
      if (nums.length > 0) customNums[fid] = nums;
    });

    const customTxts = {};
    container.querySelectorAll("[data-dyn-text]").forEach((inp) => {
      const fid = Number(inp.getAttribute("data-dyn-text"));
      if (inp.value.trim()) customTxts[fid] = inp.value.trim();
    });

    const wheelchair = container.querySelector("#profWheelchairCb").checked;
    let disabledCount = Number(container.querySelector("#profDisabledCountInput").value || 0);
    if (wheelchair && disabledCount === 0) disabledCount = 1;

    const newProfile = {
      participantAges: ages,
      minParticipantHeightCm: heightRaw ? Number(heightRaw) : null,
      requiresWheelchairAccess: wheelchair,
      disabledParticipantsCount: Math.min(ages.length, Math.max(0, disabledCount)),
      maxPhysicalDemand: container.querySelector("#profPhysicalSelect").value || null,
      explicitAdultGuardianPresent: container.querySelector("#profGuardianCb").checked ? true : null,
      requiresChildFriendly: container.querySelector("#profChildFriendlyCb").checked,
      requiresStrollerAccess: container.querySelector("#profStrollerCb").checked,
      requiresElevator: container.querySelector("#profElevatorCb").checked,
      requiredFacilityFeatureIds: reqFacilities,
      confirmedConditionFeatureIds: confConditions,
      customNumericValues: customNums,
      customOptionOrTextValues: customTxts
    };

    state.setProfile(newProfile);
    state.customGroupBreakdown = null;
    await refreshPackageEvaluation();
    showToast("Zapisano profil uczestnika / grupy!", "success");
    state.userTab = "catalog";
    await renderApp();
  });
}

// ============================================================================
// 4. ZŁOŻONE ZAMÓWIENIA UŻYTKOWNIKA
// ============================================================================

async function renderUserOrdersTab(container) {
  const orders = await api.getOrders();

  container.innerHTML = `
    <div class="section-header">
      <div>
        <h2 class="section-title">Moje złożone zamówienia (${orders.length})</h2>
        <p class="section-desc">Pełne potwierdzenia zamówień wraz z rozbiciem na kategorie zniżkowe, rabatami grupowymi i migawką cech.</p>
      </div>
    </div>

    ${
      orders.length === 0
        ? `<div class="panel">Nie złożono jeszcze żadnego zamówienia. Wybierz ofertę z katalogu, aby utworzyć pakiet.</div>`
        : orders.map((o) => `<div class="panel">${renderOrderReceiptHtml(o)}</div>`).join("")
    }
  `;
}
