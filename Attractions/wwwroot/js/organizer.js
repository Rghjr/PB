import { api } from "./api.js";
import { state } from "./state.js";
import {
  esc,
  money,
  showToast,
  openModal,
  closeModal,
  renderFeatureAssignmentsFormSection,
  collectFeatureAssignmentsFromContainer,
  renderOrderReceiptHtml
} from "./ui.js";

export async function renderOrganizerView(container, refreshGlobalData) {
  switch (state.organizerTab) {
    case "attractions":
      await renderOrgAttractions(container, refreshGlobalData);
      break;
    case "offers":
      await renderOrgOffers(container, refreshGlobalData);
      break;
    case "addonsRelations":
      await renderOrgAddonsAndRelations(container, refreshGlobalData);
      break;
    case "features":
      await renderOrgFeaturesDictionary(container, refreshGlobalData);
      break;
    case "orders":
      await renderOrgOrders(container);
      break;
  }
}

async function renderOrgAttractions(container, refreshGlobalData) {
  const attractions = await api.getAttractions(false);
  state.attractions = attractions;

  container.innerHTML = `
    <div class="section-header">
      <div>
        <h2 class="section-title">Zarządzanie atrakcjami turystycznymi</h2>
        <p class="section-desc">Definiuj obiekty, maksymalną pojemność grup jednocześnie na atrakcji oraz domyślne cechy z Features.json.</p>
      </div>
      <button class="btn btn-primary" id="orgAddAttractionBtn">+ Dodaj atrakcję</button>
    </div>

    <div class="panel">
      <div class="table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>Nazwa i opis atrakcji</th>
              <th>Lokalizacja</th>
              <th>Maks. osób</th>
              <th>Oferty</th>
              <th>Cechy ogólne obiektu</th>
              <th>Akcje</th>
            </tr>
          </thead>
          <tbody>
            ${attractions
              .map(
                (a) => `
              <tr>
                <td><strong>#${a.id}</strong></td>
                <td>
                  <div style="font-weight:700;">${esc(a.name)}</div>
                  <div class="form-hint">${esc(a.description)}</div>
                </td>
                <td>${esc(a.location)}</td>
                <td><span class="badge badge-group">${a.maxParticipants} os.</span></td>
                <td>${a.offersCount} (akt.: ${a.availableOffersCount})</td>
                <td>
                  <div class="feature-pills">
                    ${
                      a.features?.length
                        ? a.features.map((f) => `<span class="feature-pill">${esc(f.featureName)}: ${esc(f.formattedValue)}</span>`).join("")
                        : `<span class="form-hint">Brak cech</span>`
                    }
                  </div>
                </td>
                <td>
                  <div class="btn-row">
                    <button class="btn btn-sm" data-edit-attr="${a.id}">Edytuj</button>
                    <button class="btn btn-sm btn-danger" data-del-attr="${a.id}">Usuń</button>
                  </div>
                </td>
              </tr>
            `
              )
              .join("")}
          </tbody>
        </table>
      </div>
    </div>
  `;

  container.querySelector("#orgAddAttractionBtn").addEventListener("click", () =>
    openAttractionFormModal(null, refreshGlobalData, () => renderOrgAttractions(container, refreshGlobalData))
  );

  container.querySelectorAll("[data-edit-attr]").forEach((btn) => {
    btn.addEventListener("click", () => {
      const attr = attractions.find((x) => x.id === Number(btn.getAttribute("data-edit-attr")));
      if (attr) {
        openAttractionFormModal(attr, refreshGlobalData, () => renderOrgAttractions(container, refreshGlobalData));
      }
    });
  });

  container.querySelectorAll("[data-del-attr]").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const id = Number(btn.getAttribute("data-del-attr"));
      const attr = attractions.find((x) => x.id === id);
      if (!attr) return;
      if (!confirm(`Czy na pewno usunąć atrakcję „${attr.name}” wraz z jej ofertami (${attr.offersCount})?`)) return;
      try {
        const res = await api.deleteAttraction(id);
        showToast(res.message, "success");
        await refreshGlobalData();
        await renderOrgAttractions(container, refreshGlobalData);
      } catch (err) {
        showToast(err.message, "error");
      }
    });
  });
}

function openAttractionFormModal(existing, refreshGlobalData, onSaved) {
  const isEdit = Boolean(existing);
  const bodyHtml = `
    <form id="attractionModalForm">
      <div class="form-grid">
        <div class="form-group">
          <label>Nazwa atrakcji *</label>
          <input type="text" id="attrNameInput" required value="${esc(existing?.name || "")}" />
        </div>
        <div class="form-group">
          <label>Lokalizacja *</label>
          <input type="text" id="attrLocInput" required value="${esc(existing?.location || "")}" />
        </div>
        <div class="form-group">
          <label>Maksymalna liczba osób na atrakcji *</label>
          <input type="number" min="1" max="500" id="attrMaxInput" required value="${existing?.maxParticipants ?? 30}" />
        </div>
      </div>
      <div class="form-group">
        <label>Opis atrakcji</label>
        <textarea id="attrDescInput">${esc(existing?.description || "")}</textarea>
      </div>
      ${renderFeatureAssignmentsFormSection(state.features, existing?.features || [], null, false)}
    </form>
  `;

  const footerHtml = `
    <button type="button" class="btn" id="cancelModalBtn">Anuluj</button>
    <button type="button" class="btn btn-primary" id="saveAttractionBtn">${isEdit ? "Zapisz zmiany" : "Dodaj atrakcję"}</button>
  `;

  const { bodyEl, footerEl } = openModal(isEdit ? `Edycja atrakcji #${existing.id}` : "Nowa atrakcja turystyczna", bodyHtml, footerHtml);
  footerEl.querySelector("#cancelModalBtn").addEventListener("click", closeModal);

  footerEl.querySelector("#saveAttractionBtn").addEventListener("click", async () => {
    const payload = {
      name: bodyEl.querySelector("#attrNameInput").value.trim(),
      location: bodyEl.querySelector("#attrLocInput").value.trim(),
      maxParticipants: Number(bodyEl.querySelector("#attrMaxInput").value || 30),
      description: bodyEl.querySelector("#attrDescInput").value.trim(),
      features: collectFeatureAssignmentsFromContainer(bodyEl, false)
    };

    try {
      if (isEdit) {
        await api.updateAttraction(existing.id, payload);
        showToast("Zaktualizowano atrakcję i zapisano przypisane cechy.", "success");
      } else {
        await api.createAttraction(payload);
        showToast("Dodano nową atrakcję.", "success");
      }
      closeModal();
      await refreshGlobalData();
      await onSaved();
    } catch (err) {
      showToast(err.message, "error");
    }
  });
}

async function renderOrgOffers(container, refreshGlobalData) {
  const [offers, attractions] = await Promise.all([
    api.getOffers(state.orgOfferFilter),
    api.getAttractions(false)
  ]);
  state.attractions = attractions;

  const filterButtons = [
    { id: "all", label: "Wszystkie oferty" },
    { id: "planned", label: "Zaplanowane (przyszłe)" },
    { id: "today", label: "Oferty na dzisiaj" },
    { id: "group", label: "Oferty grupowe" },
    { id: "withdrawn", label: "Oferty wycofane" }
  ];

  container.innerHTML = `
    <div class="section-header">
      <div>
        <h2 class="section-title">Zarządzanie ofertami i trasami</h2>
        <p class="section-desc">Twórz oferty indywidualne i grupowe (np. ściśle dla 4 osób) oraz nadpisuj cechy atrakcji.</p>
      </div>
      <button class="btn btn-primary" id="orgAddOfferBtn">+ Dodaj nową ofertę</button>
    </div>

    <div class="filters-bar">
      <div class="filter-chips">
        ${filterButtons
          .map(
            (f) => `<button class="chip ${state.orgOfferFilter === f.id ? "active" : ""}" data-org-filter="${f.id}">${esc(f.label)}</button>`
          )
          .join("")}
      </div>
    </div>

    <div class="panel">
      <div class="table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>Oferta</th>
              <th>Atrakcja</th>
              <th>Data</th>
              <th>Cena / os.</th>
              <th>Limit grupy i miejsca</th>
              <th>Status</th>
              <th>Akcje</th>
            </tr>
          </thead>
          <tbody>
            ${offers
              .map(
                (o) => `
              <tr>
                <td><strong>#${o.id}</strong></td>
                <td>
                  <div style="font-weight:700;">${esc(o.name)}</div>
                  <div class="form-hint">Dodatki: ${o.addonsCount} • Powiązane: ${o.relatedOffersCount}</div>
                </td>
                <td>${esc(o.attractionName)}</td>
                <td>${esc(o.date)}</td>
                <td><strong>${money(o.price)}</strong></td>
                <td>
                  <span class="badge ${o.isExactGroupSizeOffer ? "badge-group-exact" : o.isGroupOffer ? "badge-group" : ""}">
                    ${esc(o.groupSizeLabel)}
                  </span>
                  <div class="form-hint">Wolne: ${o.remainingSpots}/${o.effectiveMaxParticipants} os.</div>
                </td>
                <td>
                  <span class="badge ${o.status === "Withdrawn" ? "badge-fail" : o.isToday ? "badge-today" : "badge-ok"}">
                    ${esc(o.statusLabel)}
                  </span>
                </td>
                <td>
                  <div class="btn-row">
                    <button class="btn btn-sm" data-edit-offer="${o.id}">Edytuj</button>
                    <button class="btn btn-sm" data-extras-offer="${o.id}">Dodatki/Powiązania</button>
                    ${
                      o.status !== "Withdrawn"
                        ? `<button class="btn btn-sm" data-withdraw-offer="${o.id}">Wycofaj</button>`
                        : ""
                    }
                    <button class="btn btn-sm btn-danger" data-del-offer="${o.id}">Usuń</button>
                  </div>
                </td>
              </tr>
            `
              )
              .join("")}
          </tbody>
        </table>
      </div>
    </div>
  `;

  container.querySelectorAll("[data-org-filter]").forEach((btn) => {
    btn.addEventListener("click", () => {
      state.orgOfferFilter = btn.getAttribute("data-org-filter");
      renderOrgOffers(container, refreshGlobalData);
    });
  });

  container.querySelector("#orgAddOfferBtn").addEventListener("click", () =>
    openOfferFormModal(null, attractions, refreshGlobalData, () => renderOrgOffers(container, refreshGlobalData))
  );

  container.querySelectorAll("[data-edit-offer]").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const detail = await api.getOfferDetails(Number(btn.getAttribute("data-edit-offer")), null, false);
      openOfferFormModal(detail, attractions, refreshGlobalData, () => renderOrgOffers(container, refreshGlobalData));
    });
  });

  container.querySelectorAll("[data-extras-offer]").forEach((btn) => {
    btn.addEventListener("click", () => {
      state.selectedOrgOfferIdForExtras = Number(btn.getAttribute("data-extras-offer"));
      state.organizerTab = "addonsRelations";
      document.dispatchEvent(new CustomEvent("app:rerender"));
    });
  });

  container.querySelectorAll("[data-withdraw-offer]").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const id = Number(btn.getAttribute("data-withdraw-offer"));
      if (!confirm("Czy wycofać tę ofertę? Aktywne powiązania z innymi ofertami zostaną usunięte.")) return;
      try {
        const res = await api.withdrawOffer(id);
        showToast(res.message, "success");
        await refreshGlobalData();
        await renderOrgOffers(container, refreshGlobalData);
      } catch (err) {
        showToast(err.message, "error");
      }
    });
  });

  container.querySelectorAll("[data-del-offer]").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const id = Number(btn.getAttribute("data-del-offer"));
      if (!confirm("Czy trwale usunąć tę ofertę wraz z jej dodatkami i powiązaniami?")) return;
      try {
        const res = await api.deleteOffer(id);
        showToast(res.message, "success");
        await refreshGlobalData();
        await renderOrgOffers(container, refreshGlobalData);
      } catch (err) {
        showToast(err.message, "error");
      }
    });
  });
}

function openOfferFormModal(existingDetail, attractions, refreshGlobalData, onSaved) {
  const isEdit = Boolean(existingDetail);
  const off = existingDetail?.offer;
  const todayIso = new Date().toISOString().slice(0, 10);
  const initialAttrId = off?.attractionId ?? attractions[0]?.id ?? 1;
  const initialAttr = attractions.find((a) => a.id === initialAttrId) || null;

  const bodyHtml = `
    <form id="offerModalForm">
      <div class="form-grid">
        <div class="form-group">
          <label>Przypisana atrakcja *</label>
          <select id="offAttrSelect">
            ${attractions
              .map(
                (a) => `<option value="${a.id}" ${a.id === initialAttrId ? "selected" : ""}>#${a.id} ${esc(a.name)} (maks. ${a.maxParticipants} os.)</option>`
              )
              .join("")}
          </select>
        </div>
        <div class="form-group">
          <label>Nazwa oferty *</label>
          <input type="text" id="offNameInput" required value="${esc(off?.name || "")}" />
        </div>
      </div>

      <div class="form-grid">
        <div class="form-group">
          <label>Cena podstawowa za 1 osobę (zł) *</label>
          <input type="number" step="0.01" min="0" id="offPriceInput" required value="${off?.price ?? 50}" />
        </div>
        <div class="form-group">
          <label>Data realizacji *</label>
          <input type="date" id="offDateInput" required value="${esc(off?.date || todayIso)}" />
        </div>
        ${
          isEdit
            ? `<div class="form-group">
                <label>Status oferty</label>
                <select id="offStatusSelect">
                  <option value="Planned" ${off.status === "Planned" ? "selected" : ""}>Planned (Zaplanowana)</option>
                  <option value="Withdrawn" ${off.status === "Withdrawn" ? "selected" : ""}>Withdrawn (Wycofana)</option>
                </select>
               </div>`
            : ""
        }
      </div>

      <div class="form-grid">
        <div class="form-group">
          <label>Minimalna liczba osób w grupie (np. 4 dla oferty grupowej)</label>
          <input type="number" min="1" max="500" id="offMinPartInput" value="${off?.minParticipants ?? 1}" />
        </div>
        <div class="form-group">
          <label>Maksymalna liczba osób (puste = limit z atrakcji, np. 4 dla grupy ściśle 4-os.)</label>
          <input type="number" min="1" max="500" id="offMaxPartInput" value="${off?.explicitMaxParticipants ?? ""}" placeholder="Domyślnie limit atrakcji" />
        </div>
      </div>

      <div class="form-group">
        <label>Opis oferty</label>
        <textarea id="offDescInput">${esc(off?.description || "")}</textarea>
      </div>

      <div id="offerFeaturesEditorHost">
        ${renderFeatureAssignmentsFormSection(state.features, existingDetail?.directFeatures || [], initialAttr, false)}
      </div>
    </form>
  `;

  const footerHtml = `
    <button type="button" class="btn" id="cancelOfferModalBtn">Anuluj</button>
    <button type="button" class="btn btn-primary" id="saveOfferBtn">${isEdit ? "Zapisz ofertę" : "Utwórz ofertę"}</button>
  `;

  const { bodyEl, footerEl } = openModal(isEdit ? `Edycja oferty #${off.id}` : "Dodawanie nowej oferty", bodyHtml, footerHtml);
  footerEl.querySelector("#cancelOfferModalBtn").addEventListener("click", closeModal);

  // Odśwież podgląd cech dziedziczonych po zmianie atrakcji w select
  bodyEl.querySelector("#offAttrSelect").addEventListener("change", (e) => {
    const currentPicked = collectFeatureAssignmentsFromContainer(bodyEl, false).map((x) => ({
      featureId: x.featureId,
      rawValue: x.value,
      effectMode: x.effectMode
    }));
    const newAttr = attractions.find((a) => a.id === Number(e.target.value)) || null;
    bodyEl.querySelector("#offerFeaturesEditorHost").innerHTML = renderFeatureAssignmentsFormSection(
      state.features,
      currentPicked,
      newAttr,
      false
    );
  });

  footerEl.querySelector("#saveOfferBtn").addEventListener("click", async () => {
    const maxRaw = bodyEl.querySelector("#offMaxPartInput").value.trim();
    const payload = {
      attractionId: Number(bodyEl.querySelector("#offAttrSelect").value),
      name: bodyEl.querySelector("#offNameInput").value.trim(),
      description: bodyEl.querySelector("#offDescInput").value.trim(),
      price: Number(bodyEl.querySelector("#offPriceInput").value || 0),
      date: bodyEl.querySelector("#offDateInput").value,
      status: isEdit ? bodyEl.querySelector("#offStatusSelect").value : "Planned",
      minParticipants: Number(bodyEl.querySelector("#offMinPartInput").value || 1),
      maxParticipants: maxRaw ? Number(maxRaw) : null,
      features: collectFeatureAssignmentsFromContainer(bodyEl, false)
    };

    try {
      if (isEdit) {
        await api.updateOffer(off.id, payload);
        showToast("Oferta została zaktualizowana.", "success");
      } else {
        await api.createOffer(payload);
        showToast("Dodano nową ofertę.", "success");
      }
      closeModal();
      await refreshGlobalData();
      await onSaved();
    } catch (err) {
      showToast(err.message, "error");
    }
  });
}

async function renderOrgAddonsAndRelations(container, refreshGlobalData) {
  const allOffers = await api.getOffers("all");
  if (!allOffers.length) {
    container.innerHTML = `<div class="panel">Brak ofert w systemie. Najpierw utwórz ofertę.</div>`;
    return;
  }

  if (!state.selectedOrgOfferIdForExtras || !allOffers.some((o) => o.id === state.selectedOrgOfferIdForExtras)) {
    state.selectedOrgOfferIdForExtras = allOffers[0].id;
  }

  const detail = await api.getOfferDetails(state.selectedOrgOfferIdForExtras, null, false);
  const selectedOffer = detail.offer;
  const relatedIds = new Set(detail.relatedOffers.map((r) => r.id));
  const linkCandidates = allOffers.filter(
    (o) => o.id !== selectedOffer.id && o.status === "Planned" && !relatedIds.has(o.id)
  );

  container.innerHTML = `
    <div class="section-header">
      <div>
        <h2 class="section-title">Dodatki do ofert oraz powiązania dwukierunkowe (N:M)</h2>
        <p class="section-desc">Zarządzaj dodatkami (w tym udogodnieniami jak winda/meleks lub zaostrzeniami wymagań) oraz łącz oferty w pakiety.</p>
      </div>
    </div>

    <div class="panel">
      <div class="form-group" style="margin-bottom:0;">
        <label>Wybierz ofertę do zarządzania dodatkami i powiązaniami:</label>
        <select id="orgSelectOfferForExtras">
          ${allOffers
            .map(
              (o) => `<option value="${o.id}" ${o.id === selectedOffer.id ? "selected" : ""}>#${o.id} ${esc(o.name)} (${esc(o.attractionName)} | ${esc(o.statusLabel)})</option>`
            )
            .join("")}
        </select>
      </div>
    </div>

    <div class="form-grid" style="grid-template-columns: 1fr 1fr; align-items:start;">
      <div class="panel">
        <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:12px;">
          <h3 class="panel-title" style="margin:0;">Dodatki oferty (${detail.addons.length})</h3>
          <button class="btn btn-sm btn-primary" id="addAddonBtn" ${selectedOffer.status === "Withdrawn" ? "disabled" : ""}>+ Dodaj dodatek</button>
        </div>
        ${
          detail.addons.length === 0
            ? `<p class="form-hint">Ta oferta nie posiada zdefiniowanych dodatków.</p>`
            : detail.addons
                .map(
                  (a) => `
              <div class="selectable-item">
                <div>
                  <div style="font-weight:700;">#${a.id} ${esc(a.name)} — ${money(a.price)}</div>
                  <div class="form-hint">${esc(a.description)}</div>
                  ${
                    a.featureModifiers?.length
                      ? `<div class="badges-row" style="margin-top:6px;">
                          ${a.featureModifiers
                            .map(
                              (m) => `<span class="badge ${m.effectMode === "ProvideAccommodation" ? "badge-ok" : "badge-warn"}">
                                ${m.effectMode === "ProvideAccommodation" ? "Udogodnienie" : "Wymóg"}: ${esc(m.featureName)} = ${esc(m.formattedValue)}
                              </span>`
                            )
                            .join("")}
                         </div>`
                      : ""
                  }
                </div>
                <div class="btn-row">
                  <button class="btn btn-sm" data-edit-addon="${a.id}" ${selectedOffer.status === "Withdrawn" ? "disabled" : ""}>Edytuj</button>
                  <button class="btn btn-sm btn-danger" data-del-addon="${a.id}">Usuń</button>
                </div>
              </div>
            `
                )
                .join("")
        }
      </div>

      <div class="panel">
        <h3 class="panel-title">Powiązane oferty N:M (${detail.relatedOffers.length})</h3>
        ${
          selectedOffer.status === "Withdrawn"
            ? `<div class="compat-box fail" style="margin-bottom:12px;">Oferta ma status Wycofana – nie można tworzyć dla niej powiązań.</div>`
            : `
          <div style="display:flex;gap:8px;margin-bottom:14px;">
            <select id="linkTargetOfferSelect">
              ${
                linkCandidates.length
                  ? linkCandidates.map((c) => `<option value="${c.id}">#${c.id} ${esc(c.name)} (${money(c.price)})</option>`).join("")
                  : `<option value="">Brak innych aktywnych ofert do połączenia</option>`
              }
            </select>
            <button class="btn btn-primary" id="linkOffersBtn" ${linkCandidates.length === 0 ? "disabled" : ""}>Połącz</button>
          </div>
        `
        }

        ${
          detail.relatedOffers.length === 0
            ? `<p class="form-hint">Brak powiązanych ofert.</p>`
            : detail.relatedOffers
                .map(
                  (r) => `
              <div class="selectable-item">
                <div>
                  <div style="font-weight:600;">#${r.id} ${esc(r.name)}</div>
                  <div class="form-hint">${esc(r.attractionName)} • ${esc(r.date)} • ${money(r.price)} • ${esc(r.groupSizeLabel)}</div>
                </div>
                <button class="btn btn-sm btn-danger" data-unlink-offer="${r.id}">Rozłącz</button>
              </div>
            `
                )
                .join("")
        }
      </div>
    </div>
  `;

  container.querySelector("#orgSelectOfferForExtras").addEventListener("change", (e) => {
    state.selectedOrgOfferIdForExtras = Number(e.target.value);
    renderOrgAddonsAndRelations(container, refreshGlobalData);
  });

  const addAddonBtn = container.querySelector("#addAddonBtn");
  if (addAddonBtn) {
    addAddonBtn.addEventListener("click", () =>
      openAddonFormModal(selectedOffer.id, null, () => renderOrgAddonsAndRelations(container, refreshGlobalData))
    );
  }

  container.querySelectorAll("[data-edit-addon]").forEach((btn) => {
    btn.addEventListener("click", () => {
      const addon = detail.addons.find((x) => x.id === Number(btn.getAttribute("data-edit-addon")));
      if (addon) {
        openAddonFormModal(selectedOffer.id, addon, () => renderOrgAddonsAndRelations(container, refreshGlobalData));
      }
    });
  });

  container.querySelectorAll("[data-del-addon]").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const addonId = Number(btn.getAttribute("data-del-addon"));
      if (!confirm("Usunąć ten dodatek z oferty?")) return;
      try {
        const res = await api.deleteAddon(selectedOffer.id, addonId);
        showToast(res.message, "success");
        await renderOrgAddonsAndRelations(container, refreshGlobalData);
      } catch (err) {
        showToast(err.message, "error");
      }
    });
  });

  const linkBtn = container.querySelector("#linkOffersBtn");
  if (linkBtn) {
    linkBtn.addEventListener("click", async () => {
      const targetId = Number(container.querySelector("#linkTargetOfferSelect").value);
      if (!targetId) return;
      try {
        const res = await api.linkOffers(selectedOffer.id, targetId);
        showToast(res.message, "success");
        await renderOrgAddonsAndRelations(container, refreshGlobalData);
      } catch (err) {
        showToast(err.message, "error");
      }
    });
  }

  container.querySelectorAll("[data-unlink-offer]").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const secondId = Number(btn.getAttribute("data-unlink-offer"));
      try {
        const res = await api.unlinkOffers(selectedOffer.id, secondId);
        showToast(res.message, "success");
        await renderOrgAddonsAndRelations(container, refreshGlobalData);
      } catch (err) {
        showToast(err.message, "error");
      }
    });
  });
}

function openAddonFormModal(offerId, existingAddon, onSaved) {
  const isEdit = Boolean(existingAddon);
  const bodyHtml = `
    <form id="addonModalForm">
      <div class="form-grid">
        <div class="form-group">
          <label>Nazwa dodatku *</label>
          <input type="text" id="addonNameInput" required value="${esc(existingAddon?.name || "")}" />
        </div>
        <div class="form-group">
          <label>Cena dodatku (zł) *</label>
          <input type="number" step="0.01" min="0" id="addonPriceInput" required value="${existingAddon?.price ?? 25}" />
        </div>
      </div>
      <div class="form-group">
        <label>Opis dodatku</label>
        <textarea id="addonDescInput">${esc(existingAddon?.description || "")}</textarea>
      </div>
      ${renderFeatureAssignmentsFormSection(state.features, existingAddon?.featureModifiers || [], null, true)}
    </form>
  `;

  const footerHtml = `
    <button type="button" class="btn" id="cancelAddonBtn">Anuluj</button>
    <button type="button" class="btn btn-primary" id="saveAddonBtn">${isEdit ? "Zapisz dodatek" : "Dodaj dodatek"}</button>
  `;

  const { bodyEl, footerEl } = openModal(isEdit ? `Edycja dodatku #${existingAddon.id}` : "Nowy dodatek do oferty", bodyHtml, footerHtml);
  footerEl.querySelector("#cancelAddonBtn").addEventListener("click", closeModal);

  footerEl.querySelector("#saveAddonBtn").addEventListener("click", async () => {
    const payload = {
      name: bodyEl.querySelector("#addonNameInput").value.trim(),
      description: bodyEl.querySelector("#addonDescInput").value.trim(),
      price: Number(bodyEl.querySelector("#addonPriceInput").value || 0),
      featureModifiers: collectFeatureAssignmentsFromContainer(bodyEl, true)
    };

    try {
      if (isEdit) {
        await api.updateAddon(offerId, existingAddon.id, payload);
        showToast("Zaktualizowano dodatek.", "success");
      } else {
        await api.createAddon(offerId, payload);
        showToast("Dodano nowy dodatek do oferty.", "success");
      }
      closeModal();
      await onSaved();
    } catch (err) {
      showToast(err.message, "error");
    }
  });
}

async function renderOrgFeaturesDictionary(container, refreshGlobalData) {
  const features = await api.getFeatures();
  state.features = features;

  const ruleNames = {
    FacilityAvailable: "Udogodnienie (dostępne = Tak)",
    ConditionRequired: "Warunek wymagany od uczestnika",
    MinThreshold: "Próg minimalny (uczestnik >= wartość)",
    MaxThreshold: "Próg maksymalny (uczestnik <= wartość)",
    MaxLevelIndex: "Maksymalny poziom trudności/wysiłku",
    ExactOrContainsMatch: "Dopasowanie tekstu / opcji"
  };

  container.innerHTML = `
    <div class="section-header">
      <div>
        <h2 class="section-title">Słownik cech i wymagań (Features.json)</h2>
        <p class="section-desc">Dynamiczny słownik JSON. Każda nowo dodana cecha od razu działa w atrakcjach, ofertach, dodatkach i profilu uczestnika bez zmian w kodzie C#.</p>
      </div>
      <button class="btn btn-primary" id="addFeatureBtn">+ Dodaj cechę do Features.json</button>
    </div>

    <div class="panel">
      <div class="table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>Nazwa i opis cechy</th>
              <th>Typ danych</th>
              <th>Reguła weryfikacji i agregacji</th>
              <th>Użycia</th>
              <th>Akcje</th>
            </tr>
          </thead>
          <tbody>
            ${features
              .map(
                (f) => `
              <tr>
                <td><strong>#${f.id}</strong></td>
                <td>
                  <div style="font-weight:700;">${esc(f.name)}</div>
                  <div class="form-hint">${esc(f.description)}</div>
                </td>
                <td>
                  <span class="badge">${esc(f.type)}${f.unit ? ` (${esc(f.unit)})` : ""}</span>
                  ${f.allowedValues?.length ? `<div class="form-hint">Opcje: ${esc(f.allowedValues.join(", "))}</div>` : ""}
                </td>
                <td>${esc(ruleNames[f.comparisonRule] || f.comparisonRule)}</td>
                <td><strong>${f.usageCount}</strong> ob.</td>
                <td>
                  <div class="btn-row">
                    <button class="btn btn-sm" data-edit-feat="${f.id}">Edytuj</button>
                    <button class="btn btn-sm btn-danger" data-del-feat="${f.id}">Usuń</button>
                  </div>
                </td>
              </tr>
            `
              )
              .join("")}
          </tbody>
        </table>
      </div>
    </div>
  `;

  container.querySelector("#addFeatureBtn").addEventListener("click", () =>
    openFeatureFormModal(null, refreshGlobalData, () => renderOrgFeaturesDictionary(container, refreshGlobalData))
  );

  container.querySelectorAll("[data-edit-feat]").forEach((btn) => {
    btn.addEventListener("click", () => {
      const feat = features.find((x) => x.id === Number(btn.getAttribute("data-edit-feat")));
      if (feat) {
        openFeatureFormModal(feat, refreshGlobalData, () => renderOrgFeaturesDictionary(container, refreshGlobalData));
      }
    });
  });

  container.querySelectorAll("[data-del-feat]").forEach((btn) => {
    btn.addEventListener("click", async () => {
      const id = Number(btn.getAttribute("data-del-feat"));
      const feat = features.find((x) => x.id === id);
      if (!feat) return;

      try {
        const res = await api.deleteFeature(id, false);
        showToast(res.message, "success");
        await refreshGlobalData();
        await renderOrgFeaturesDictionary(container, refreshGlobalData);
      } catch (err) {
        if (err.status === 409 && err.payload?.details) {
          const usage = err.payload.details;
          openCascadeDeleteFeatureModal(feat, usage, refreshGlobalData, () =>
            renderOrgFeaturesDictionary(container, refreshGlobalData)
          );
        } else {
          showToast(err.message, "error");
        }
      }
    });
  });
}

function openCascadeDeleteFeatureModal(feat, usage, refreshGlobalData, onDeleted) {
  const bodyHtml = `
    <div class="compat-box fail" style="margin-bottom:14px;">
      <strong>Ostrzeżenie: Cecha „${esc(feat.name)}” jest obecnie wykorzystywana przez ${usage.totalCount} obiektów!</strong>
      <p style="margin:6px 0 0;">Usunięcie tej cechy ze słownika Features.json spowoduje jednoczesne odpięcie jej z poniższych elementów:</p>
    </div>
    <ul style="font-size:15.5px;">
      <li><strong>Atrakcje (${usage.attractions.length}):</strong> ${esc(usage.attractions.map((x) => x.name).join(", ") || "brak")}</li>
      <li><strong>Oferty (${usage.offers.length}):</strong> ${esc(usage.offers.map((x) => x.name).join(", ") || "brak")}</li>
      <li><strong>Dodatki (${usage.addons.length}):</strong> ${esc(usage.addons.map((x) => x.name).join(", ") || "brak")}</li>
    </ul>
  `;

  const footerHtml = `
    <button type="button" class="btn" id="cancelCascadeBtn">Anuluj</button>
    <button type="button" class="btn btn-danger" id="confirmCascadeBtn">Usuń cechę kaskadowo</button>
  `;

  const { footerEl } = openModal(`Usuwanie używanej cechy #${feat.id}`, bodyHtml, footerHtml);
  footerEl.querySelector("#cancelCascadeBtn").addEventListener("click", closeModal);
  footerEl.querySelector("#confirmCascadeBtn").addEventListener("click", async () => {
    try {
      const res = await api.deleteFeature(feat.id, true);
      showToast(res.message, "success");
      closeModal();
      await refreshGlobalData();
      await onDeleted();
    } catch (err) {
      showToast(err.message, "error");
    }
  });
}

function openFeatureFormModal(existing, refreshGlobalData, onSaved) {
  const isEdit = Boolean(existing);
  const currentType = existing?.type || "Boolean";

  const bodyHtml = `
    <form id="featureModalForm">
      <div class="form-grid">
        <div class="form-group">
          <label>Nazwa cechy *</label>
          <input type="text" id="featNameInput" required value="${esc(existing?.name || "")}" placeholder="np. Maksymalna waga uczestnika" />
        </div>
        <div class="form-group">
          <label>Typ wartości *</label>
          <select id="featTypeSelect" ${isEdit ? "disabled" : ""}>
            <option value="Boolean" ${currentType === "Boolean" ? "selected" : ""}>Boolean (Tak / Nie)</option>
            <option value="Number" ${currentType === "Number" ? "selected" : ""}>Number (Liczba)</option>
            <option value="OptionList" ${currentType === "OptionList" ? "selected" : ""}>OptionList (Lista opcji, np. Niski, Średni, Wysoki)</option>
            <option value="Text" ${currentType === "Text" ? "selected" : ""}>Text (Tekst)</option>
          </select>
        </div>
      </div>

      <div class="form-group">
        <label>Opis cechy</label>
        <input type="text" id="featDescInput" value="${esc(existing?.description || "")}" />
      </div>

      <div class="form-group" id="featRuleGroup">
        <label>Reguła weryfikacji z profilem uczestnika</label>
        <select id="featRuleSelect"></select>
      </div>

      <div class="form-group" id="featUnitGroup">
        <label>Jednostka (opcjonalnie dla Number, np. lat, cm, kg)</label>
        <input type="text" id="featUnitInput" value="${esc(existing?.unit || "")}" />
      </div>

      <div class="form-group" id="featOptionsGroup">
        <label>Dozwolone wartości po przecinku (od najmniej do najbardziej wymagającej) *</label>
        <input type="text" id="featOptionsInput" value="${esc(existing?.allowedValues?.join(", ") || "Niski, Średni, Wysoki")}" />
      </div>
    </form>
  `;

  const footerHtml = `
    <button type="button" class="btn" id="cancelFeatBtn">Anuluj</button>
    <button type="button" class="btn btn-primary" id="saveFeatBtn">${isEdit ? "Zapisz w Features.json" : "Dodaj do Features.json"}</button>
  `;

  const { bodyEl, footerEl } = openModal(isEdit ? `Edycja cechy #${existing.id}` : "Nowa cecha w słowniku Features.json", bodyHtml, footerHtml);
  footerEl.querySelector("#cancelFeatBtn").addEventListener("click", closeModal);

  const typeSelect = bodyEl.querySelector("#featTypeSelect");
  const ruleSelect = bodyEl.querySelector("#featRuleSelect");
  const unitGroup = bodyEl.querySelector("#featUnitGroup");
  const optionsGroup = bodyEl.querySelector("#featOptionsGroup");

  function syncTypeFields() {
    const t = typeSelect.value;
    unitGroup.classList.toggle("hidden", t !== "Number");
    optionsGroup.classList.toggle("hidden", t !== "OptionList");

    let ruleOptions = [];
    if (t === "Boolean") {
      ruleOptions = [
        { val: "FacilityAvailable", label: "Udogodnienie / dostępność (np. wózek, winda, pętla indukcyjna)" },
        { val: "ConditionRequired", label: "Warunek wymagany od uczestnika (np. opiekun, obuwie górskie)" }
      ];
    } else if (t === "Number") {
      ruleOptions = [
        { val: "MinThreshold", label: "Próg minimalny – uczestnik >= wartość cechy (np. min. wiek, wzrost)" },
        { val: "MaxThreshold", label: "Próg maksymalny – uczestnik <= wartość cechy (np. maks. wiek, waga)" }
      ];
    } else if (t === "OptionList") {
      ruleOptions = [
        { val: "MaxLevelIndex", label: "Uporządkowane poziomy trudności/intensywności" },
        { val: "ExactOrContainsMatch", label: "Dopasowanie konkretnego wariantu" }
      ];
    } else {
      ruleOptions = [{ val: "ExactOrContainsMatch", label: "Dopasowanie frazy / informacyjne" }];
    }

    ruleSelect.innerHTML = ruleOptions
      .map(
        (r) => `<option value="${r.val}" ${existing?.comparisonRule === r.val ? "selected" : ""}>${esc(r.label)}</option>`
      )
      .join("");
  }

  typeSelect.addEventListener("change", syncTypeFields);
  syncTypeFields();

  footerEl.querySelector("#saveFeatBtn").addEventListener("click", async () => {
    const t = typeSelect.value;
    const rawOpts = bodyEl.querySelector("#featOptionsInput").value;
    const payload = {
      name: bodyEl.querySelector("#featNameInput").value.trim(),
      type: t,
      description: bodyEl.querySelector("#featDescInput").value.trim(),
      comparisonRule: ruleSelect.value,
      unit: t === "Number" ? bodyEl.querySelector("#featUnitInput").value.trim() : null,
      allowedValues:
        t === "OptionList"
          ? rawOpts
              .split(",")
              .map((s) => s.trim())
              .filter(Boolean)
          : null
    };

    try {
      if (isEdit) {
        await api.updateFeature(existing.id, payload);
        showToast("Zaktualizowano cechę w Features.json.", "success");
      } else {
        await api.createFeature(payload);
        showToast("Dodano nową cechę do Features.json!", "success");
      }
      closeModal();
      await refreshGlobalData();
      await onSaved();
    } catch (err) {
      showToast(err.message, "error");
    }
  });
}

async function renderOrgOrders(container) {
  const orders = await api.getOrders();

  container.innerHTML = `
    <div class="section-header">
      <div>
        <h2 class="section-title">Złożone zamówienia klientów (${orders.length})</h2>
        <p class="section-desc">Podgląd rezerwacji indywidualnych i grupowych wraz z naliczonymi rabatami.</p>
      </div>
    </div>

    <div class="panel">
      ${
        orders.length === 0
          ? `<p class="form-hint">Nie złożono jeszcze żadnych zamówień w bieżącej sesji serwera.</p>`
          : `
        <div class="table-wrap">
          <table class="data-table">
            <thead>
              <tr>
                <th>Nr zamówienia</th>
                <th>Data złożenia</th>
                <th>Zamawiający</th>
                <th>Uczestnicy</th>
                <th>Pozycje</th>
                <th>Zniżki łącznie</th>
                <th>Kwota końcowa</th>
                <th>Szczegóły</th>
              </tr>
            </thead>
            <tbody>
              ${orders
                .map(
                  (o) => `
                <tr>
                  <td><strong>${esc(o.orderNumber)}</strong></td>
                  <td>${esc(o.createdAt)}</td>
                  <td>
                    <div style="font-weight:600;">${esc(o.customerName)}</div>
                    <div class="form-hint">${esc(o.customerEmail || "brak e-maila")}</div>
                  </td>
                  <td>${o.ticketsCount} os.</td>
                  <td>${o.items.length}</td>
                  <td style="color:var(--success);font-weight:600;">${o.totalSavings > 0 ? `-${money(o.totalSavings)}` : "0,00 zł"}</td>
                  <td><strong>${money(o.totalPrice)}</strong></td>
                  <td><button class="btn btn-sm" data-view-order="${o.id}">Potwierdzenie</button></td>
                </tr>
              `
                )
                .join("")}
            </tbody>
          </table>
        </div>
      `
      }
    </div>
  `;

  container.querySelectorAll("[data-view-order]").forEach((btn) => {
    btn.addEventListener("click", () => {
      const ord = orders.find((x) => x.id === Number(btn.getAttribute("data-view-order")));
      if (ord) {
        openModal(`Zamówienie ${ord.orderNumber}`, renderOrderReceiptHtml(ord));
      }
    });
  });
}
