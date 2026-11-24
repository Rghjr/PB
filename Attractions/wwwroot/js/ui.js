export function esc(value) {
  if (value === null || value === undefined) return "";
  return String(value)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

export function money(value) {
  const num = Number(value || 0);
  return num.toLocaleString("pl-PL", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + " zł";
}

export function showToast(message, type = "info") {
  const stack = document.getElementById("toastStack");
  const el = document.createElement("div");
  el.className = `toast ${type}`;
  el.textContent = message;
  stack.appendChild(el);
  setTimeout(() => {
    el.remove();
  }, 4200);
}

export function openModal(title, bodyHtml, footerHtml = "") {
  const backdrop = document.getElementById("modalBackdrop");
  const titleEl = document.getElementById("modalTitle");
  const bodyEl = document.getElementById("modalBody");
  const footerEl = document.getElementById("modalFooter");

  titleEl.textContent = title;
  bodyEl.innerHTML = bodyHtml;

  if (footerHtml) {
    footerEl.innerHTML = footerHtml;
    footerEl.classList.remove("hidden");
  } else {
    footerEl.innerHTML = "";
    footerEl.classList.add("hidden");
  }

  backdrop.classList.remove("hidden");
  return { bodyEl, footerEl };
}

export function closeModal() {
  document.getElementById("modalBackdrop").classList.add("hidden");
}

export function renderCompatibilityBox(report, compact = false) {
  if (!report) return "";

  if (report.isCompatible) {
    const metHtml =
      !compact && report.metRequirements?.length
        ? `<div style="margin-top:8px;font-weight:600;">Spełnione wymagania (${report.metRequirements.length}):</div>
           <ul>${report.metRequirements.map((m) => `<li>${esc(m.formattedMessage || m.message)}</li>`).join("")}</ul>`
        : "";

    const warnHtml =
      report.warnings?.length
        ? `<div style="margin-top:8px;color:#92400e;font-weight:600;">Uwagi do weryfikacji:</div>
           <ul>${report.warnings.map((w) => `<li>${esc(w.formattedMessage || w.message)}</li>`).join("")}</ul>`
        : "";

    return `
      <div class="compat-box ok">
        <strong>MOŻNA SKORZYSTAĆ Z ${esc(report.targetTypeLabel)}: ${esc(report.targetName)}</strong>
        ${warnHtml}
        ${metHtml}
      </div>
    `;
  }

  const reasonsHtml = (report.unmetRequirements || [])
    .map((u) => `<li>${esc(u.formattedMessage || u.message)}</li>`)
    .join("");

  const metHtml =
    !compact && report.metRequirements?.length
      ? `<div style="margin-top:10px;color:#166534;font-weight:600;">Spełnione wymagania:</div>
         <ul style="color:#166534;">${report.metRequirements.map((m) => `<li>${esc(m.formattedMessage || m.message)}</li>`).join("")}</ul>`
      : "";

  return `
    <div class="compat-box fail">
      <strong>NIE MOŻNA SKORZYSTAĆ Z ${esc(report.targetTypeLabel)}: ${esc(report.targetName)}</strong>
      <div style="margin-top:6px;font-weight:600;">Powody braku kompatybilności:</div>
      <ul>${reasonsHtml}</ul>
      ${metHtml}
    </div>
  `;
}

export function renderFeatureAssignmentsFormSection(features, currentAssignments = [], inheritedAttraction = null, isAddon = false) {
  const currentMap = new Map();
  (currentAssignments || []).forEach((a) => {
    currentMap.set(a.featureId, {
      value: a.rawValue !== undefined ? a.rawValue : a.value,
      effectMode: a.effectMode || "TightenRequirement"
    });
  });

  const inheritedMap = new Map();
  if (inheritedAttraction?.features) {
    inheritedAttraction.features.forEach((f) => {
      inheritedMap.set(f.featureId, f.formattedValue || f.rawValue);
    });
  }

  if (!features.length) {
    return `<p class="form-hint">Słownik Features.json nie zawiera jeszcze żadnych cech.</p>`;
  }

  return `
    <div style="border-top:1px solid var(--border);margin-top:12px;padding-top:12px;">
      <div style="font-weight:700;margin-bottom:6px;">
        ${isAddon ? "Wpływ dodatku na cechy pakietu (Features.json)" : "Przypisane cechy i wymagania ze słownika Features.json"}
      </div>
      <div class="form-hint" style="margin-bottom:10px;">
        ${
          inheritedAttraction
            ? `Zaznacz cechy, które chcesz ustawić lub nadpisać względem atrakcji „${esc(inheritedAttraction.name)}”.`
            : "Zaznacz wybrane cechy i określ ich wartości. Nowo dodane cechy w słowniku pojawiają się tu automatycznie."
        }
      </div>
      <div class="table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th>Ustaw</th>
              <th>Cecha</th>
              <th>Wartość</th>
              ${isAddon ? "<th>Tryb wpływu dodatku</th>" : "<th>Domyślnie z atrakcji</th>"}
            </tr>
          </thead>
          <tbody>
            ${features
              .map((f) => {
                const assigned = currentMap.get(f.id);
                const isChecked = assigned !== undefined;
                const val = assigned ? String(assigned.value) : "";
                const inh = inheritedMap.get(f.id);

                let inputControl = "";
                if (f.type === "Boolean") {
                  inputControl = `
                    <select data-feat-val="${f.id}">
                      <option value="true" ${val === "true" ? "selected" : ""}>Tak (true)</option>
                      <option value="false" ${val === "false" ? "selected" : ""}>Nie (false)</option>
                    </select>`;
                } else if (f.type === "OptionList" && f.allowedValues?.length) {
                  inputControl = `
                    <select data-feat-val="${f.id}">
                      ${f.allowedValues
                        .map((opt) => `<option value="${esc(opt)}" ${val.toLowerCase() === opt.toLowerCase() ? "selected" : ""}>${esc(opt)}</option>`)
                        .join("")}
                    </select>`;
                } else if (f.type === "Number") {
                  inputControl = `<input type="number" step="any" data-feat-val="${f.id}" value="${esc(val || "0")}" placeholder="${esc(f.unit || "liczba")}" />`;
                } else {
                  inputControl = `<input type="text" data-feat-val="${f.id}" value="${esc(val)}" placeholder="Wartość tekstowa" />`;
                }

                const extraCol = isAddon
                  ? `<td>
                      <select data-feat-mode="${f.id}">
                        <option value="TightenRequirement" ${assigned?.effectMode !== "ProvideAccommodation" ? "selected" : ""}>Zaostrza wymaganie</option>
                        <option value="ProvideAccommodation" ${assigned?.effectMode === "ProvideAccommodation" ? "selected" : ""}>Zapewnia udogodnienie</option>
                      </select>
                     </td>`
                  : `<td class="form-hint">${inh ? esc(inh) : "—"}</td>`;

                return `
                  <tr>
                    <td><input type="checkbox" data-feat-enable="${f.id}" ${isChecked ? "checked" : ""} /></td>
                    <td>
                      <div style="font-weight:600;">${esc(f.name)}</div>
                      <div class="form-hint">${esc(f.type)}${f.unit ? ` (${esc(f.unit)})` : ""}</div>
                    </td>
                    <td>${inputControl}</td>
                    ${extraCol}
                  </tr>
                `;
              })
              .join("")}
          </tbody>
        </table>
      </div>
    </div>
  `;
}

export function collectFeatureAssignmentsFromContainer(container, isAddon = false) {
  const result = [];
  const checkboxes = container.querySelectorAll("[data-feat-enable]");
  checkboxes.forEach((cb) => {
    if (!cb.checked) return;
    const featureId = Number(cb.getAttribute("data-feat-enable"));
    const valEl = container.querySelector(`[data-feat-val="${featureId}"]`);
    const modeEl = isAddon ? container.querySelector(`[data-feat-mode="${featureId}"]`) : null;
    result.push({
      featureId,
      value: valEl ? String(valEl.value).trim() : "",
      effectMode: modeEl ? modeEl.value : "TightenRequirement"
    });
  });
  return result;
}

export function renderOrderReceiptHtml(order) {
  const itemsHtml = (order.items || [])
    .map(
      (item) => `
      <div class="panel" style="margin-bottom:10px;padding:12px 14px;background:var(--surface-muted);">
        <div style="display:flex;justify-content:space-between;gap:10px;flex-wrap:wrap;">
          <div>
            <span class="badge ${item.isMainOffer ? "badge-group" : ""}">${item.isMainOffer ? "Oferta główna" : "Oferta powiązana"}</span>
            <strong style="margin-left:6px;">#${item.offerId} ${esc(item.offerName)}</strong>
            <div class="form-hint">${esc(item.attractionName)} • Termin: ${esc(item.date)}</div>
          </div>
          <div style="text-align:right;">
            <div>Cena baz.: ${money(item.basePrice)}</div>
            <div style="font-weight:700;">Podsuma: ${money(item.itemSubtotal)}</div>
          </div>
        </div>
        ${
          item.selectedAddons?.length
            ? `<div style="margin-top:8px;font-size:15px;">
                <strong>Dodatki:</strong>
                ${item.selectedAddons.map((a) => `${esc(a.name)} (+${money(a.price)})`).join(" • ")}
               </div>`
            : ""
        }
        ${
          item.effectiveFeaturesSnapshot?.length
            ? `<div class="feature-pills" style="margin-top:8px;">
                ${item.effectiveFeaturesSnapshot.map((s) => `<span class="feature-pill">${esc(s)}</span>`).join("")}
               </div>`
            : ""
        }
      </div>
    `
    )
    .join("");

  const groupsHtml = (order.participantGroups || [])
    .map(
      (g) => `
      <div class="price-line">
        <span>${esc(g.categoryName)} &times; ${g.count} (${g.discountPercent > 0 ? `-${g.discountPercent}%` : "0%"})</span>
        <span>${money(g.lineSubtotal)}</span>
      </div>
    `
    )
    .join("");

  return `
    <div>
      <div class="compat-box ok" style="margin-bottom:14px;">
        <strong>POTWIERDZENIE ZAMÓWIENIA NR ${esc(order.orderNumber)}</strong>
        <div>Złożono: ${esc(order.createdAt)} • Płatność: ${esc(order.paymentMethodLabel)}</div>
        <div>Zamawiający: <strong>${esc(order.customerName)}</strong> ${order.customerEmail ? `(${esc(order.customerEmail)})` : ""}</div>
      </div>

      <h4 style="margin:12px 0 8px;">Pozycje pakietu (na 1 osobę: ${money(order.pricePerPerson)})</h4>
      ${itemsHtml}

      <h4 style="margin:14px 0 8px;">Rozliczenie grupy (${order.ticketsCount} os.) i zniżki</h4>
      <div class="panel" style="margin-bottom:0;">
        <div class="price-summary-lines">
          ${groupsHtml}
          <div class="price-line" style="border-top:1px solid var(--border);padding-top:6px;">
            <span>Wartość regularna bez zniżek:</span>
            <span>${money(order.regularGrossPrice)}</span>
          </div>
          ${
            order.categoryDiscountsSavings > 0
              ? `<div class="price-line savings">
                  <span>Zniżki ulgowe kategorii:</span>
                  <span>-${money(order.categoryDiscountsSavings)}</span>
                 </div>`
              : ""
          }
          ${
            order.extraGroupDiscountAmount > 0
              ? `<div class="price-line savings">
                  <span>Dodatkowy rabat grupowy (-${order.extraGroupDiscountPercent}%):</span>
                  <span>-${money(order.extraGroupDiscountAmount)}</span>
                 </div>`
              : ""
          }
          <div class="price-line total">
            <span>RAZEM DO ZAPŁATY:</span>
            <span>${money(order.totalPrice)}</span>
          </div>
        </div>
      </div>
    </div>
  `;
}
