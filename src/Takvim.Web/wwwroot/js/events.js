/*
 * Takvim+ — Etkinlik işlemleri (ortak modül)
 *
 * - Web uygulamasının /Events/Data uç noktaları üzerinden
 *   Takvim+ API'sine CRUD istekleri gönderir.
 * - Etkinlik ekleme/düzenleme penceresini (Bootstrap modal) yönetir.
 *
 * Zamanlar tarayıcının yerel saat diliminde gösterilir,
 * API'ye ISO 8601 UTC olarak gönderilir.
 * Tüm gün etkinliklerinde bitiş zamanı hariç tutulur
 * (ertesi günün 00:00'ı).
 */
window.TakvimEvents = (() => {
    const BASE_URL = "/Events/Data";
    const DAY_MS = 24 * 60 * 60 * 1000;

    // ---------------------------------------------------------------
    // API istemcisi
    // ---------------------------------------------------------------

    function antiForgeryToken() {
        return document.querySelector(
            '#eventAntiForgery input[name="__RequestVerificationToken"]'
        )?.value ?? "";
    }

    async function request(method, url, body) {
        const headers = { "Accept": "application/json" };

        if (method !== "GET") {
            headers["RequestVerificationToken"] = antiForgeryToken();
        }

        if (body !== undefined) {
            headers["Content-Type"] = "application/json";
        }

        const response = await fetch(url, {
            method,
            headers,
            credentials: "same-origin",
            body: body === undefined ? undefined : JSON.stringify(body)
        });

        if (response.status === 401) {
            window.location.href = "/Account/Login";
            throw new Error("Oturum süresi doldu.");
        }

        if (response.status === 204) {
            return null;
        }

        const data = await response.json().catch(() => null);

        if (!response.ok) {
            const error = new Error(problemMessage(data, response.status));
            error.status = response.status;
            throw error;
        }

        return data;
    }

    // ValidationProblemDetails / ProblemDetails yanıtını okunur metne çevirir
    function problemMessage(data, status) {
        if (data?.errors) {
            const messages = Object.values(data.errors).flat();
            if (messages.length) return messages.join(" ");
        }

        if (data?.title) return data.title;
        if (status === 404) return "Etkinlik bulunamadı.";

        return "İşlem gerçekleştirilemedi. Lütfen tekrar deneyin.";
    }

    const api = {
        list(from, to) {
            const params = new URLSearchParams();
            if (from) params.set("from", from.toISOString());
            if (to) params.set("to", to.toISOString());

            const query = params.toString();
            return request("GET", query ? `${BASE_URL}?${query}` : BASE_URL);
        },
        get: (id) => request("GET", `${BASE_URL}/${id}`),
        create: (body) => request("POST", BASE_URL, body),
        update: (id, body) => request("PUT", `${BASE_URL}/${id}`, body),
        remove: (id) => request("DELETE", `${BASE_URL}/${id}`)
    };

    // ---------------------------------------------------------------
    // Tarih yardımcıları
    // ---------------------------------------------------------------

    const pad = (n) => String(n).padStart(2, "0");

    const startOfDay = (d) =>
        new Date(d.getFullYear(), d.getMonth(), d.getDate());

    const addDays = (d, n) =>
        new Date(d.getFullYear(), d.getMonth(), d.getDate() + n);

    const toDateInput = (d) =>
        `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;

    const toTimeInput = (d) => `${pad(d.getHours())}:${pad(d.getMinutes())}`;

    function fromInputs(dateValue, timeValue) {
        const [y, m, d] = dateValue.split("-").map(Number);
        const [hh, mm] = (timeValue || "00:00").split(":").map(Number);
        return new Date(y, m - 1, d, hh, mm);
    }

    const startOf = (ev) => new Date(ev.startAtUtc);
    const endOf = (ev) => new Date(ev.endAtUtc);

    // Etkinlik verilen günle kesişiyor mu? (bitiş hariç)
    function occursOn(ev, day) {
        const dayStart = startOfDay(day);
        const dayEnd = addDays(dayStart, 1);
        const start = startOf(ev);
        const end = endOf(ev);

        return start < dayEnd && (end > dayStart || start >= dayStart);
    }

    const timeFormatter = new Intl.DateTimeFormat("tr-TR", {
        hour: "2-digit",
        minute: "2-digit"
    });

    const shortDateFormatter = new Intl.DateTimeFormat("tr-TR", {
        day: "numeric",
        month: "short"
    });

    function isSameDay(a, b) {
        return a.getFullYear() === b.getFullYear() &&
            a.getMonth() === b.getMonth() &&
            a.getDate() === b.getDate();
    }

    // "09:00 – 10:30", "Tüm gün", "25 Kas 22:00 – 26 Kas 01:00" gibi
    function formatTimeRange(ev) {
        const start = startOf(ev);
        const end = endOf(ev);

        if (ev.isAllDay) {
            const lastDay = addDays(startOfDay(end), -1);
            return lastDay > start
                ? `Tüm gün · ${shortDateFormatter.format(start)} – ${shortDateFormatter.format(lastDay)}`
                : "Tüm gün";
        }

        if (isSameDay(start, end)) {
            return `${timeFormatter.format(start)} – ${timeFormatter.format(end)}`;
        }

        return `${shortDateFormatter.format(start)} ${timeFormatter.format(start)} – ` +
            `${shortDateFormatter.format(end)} ${timeFormatter.format(end)}`;
    }

    // Etkinliğin rengi: kategori varsa kategori, yoksa takvim rengi
    const colorOf = (ev) => ev.categoryColor || ev.calendarColor || "#3563E9";

    // ---------------------------------------------------------------
    // Ekle / düzenle penceresi
    // ---------------------------------------------------------------

    let modal = null;
    let els = null;
    let editingEvent = null;
    let onChange = () => { };

    function initModal(changeCallback) {
        const modalEl = document.getElementById("eventModal");
        if (!modalEl) return;

        onChange = changeCallback ?? onChange;
        modal = bootstrap.Modal.getOrCreateInstance(modalEl);

        els = {
            modal: modalEl,
            form: document.getElementById("eventForm"),
            eyebrow: document.getElementById("eventModalEyebrow"),
            heading: document.getElementById("eventModalTitle"),
            error: document.getElementById("eventFormError"),
            rangeError: document.getElementById("eventRangeError"),
            title: document.getElementById("eventTitle"),
            allDay: document.getElementById("eventAllDay"),
            startDate: document.getElementById("eventStartDate"),
            startTime: document.getElementById("eventStartTime"),
            endDate: document.getElementById("eventEndDate"),
            endTime: document.getElementById("eventEndTime"),
            location: document.getElementById("eventLocation"),
            description: document.getElementById("eventDescription"),
            save: document.getElementById("eventSaveBtn"),
            remove: document.getElementById("eventDeleteBtn")
        };

        els.allDay.addEventListener("change", syncAllDay);
        els.form.addEventListener("submit", onSubmit);
        els.remove.addEventListener("click", onDelete);

        // Başlangıç tarihi değişince bitiş tarihini de kaydır
        els.startDate.addEventListener("change", () => {
            if (els.endDate.value < els.startDate.value) {
                els.endDate.value = els.startDate.value;
            }
        });

        modalEl.addEventListener("shown.bs.modal", () => els.title.focus());
    }

    function syncAllDay() {
        const allDay = els.allDay.checked;

        for (const input of [els.startTime, els.endTime]) {
            input.hidden = allDay;
            input.required = !allDay;
        }
    }

    function setBusy(busy) {
        els.save.disabled = busy;
        els.remove.disabled = busy;
        els.save.textContent = busy ? "Kaydediliyor…" : "Kaydet";
    }

    function showError(message) {
        els.error.textContent = message;
        els.error.hidden = !message;
    }

    function resetValidation() {
        showError("");
        els.rangeError.hidden = true;
        els.form.classList.remove("was-validated");
    }

    // Yeni etkinlik: seçili gün için varsayılan saat önerir
    function openCreate(date) {
        editingEvent = null;
        resetValidation();

        const now = new Date();
        const day = startOfDay(date ?? now);

        let start = new Date(day.getFullYear(), day.getMonth(), day.getDate(), 9, 0);

        if (isSameDay(day, now)) {
            start = new Date(day.getFullYear(), day.getMonth(), day.getDate(),
                Math.min(now.getHours() + 1, 23), 0);
        }

        const end = new Date(start.getTime() + 60 * 60 * 1000);

        els.eyebrow.textContent = "YENİ ETKİNLİK";
        els.heading.textContent = "Etkinlik oluştur";
        els.title.value = "";
        els.allDay.checked = false;
        els.startDate.value = toDateInput(start);
        els.startTime.value = toTimeInput(start);
        els.endDate.value = toDateInput(end);
        els.endTime.value = toTimeInput(end);
        els.location.value = "";
        els.description.value = "";
        els.remove.hidden = true;

        syncAllDay();
        setBusy(false);
        modal.show();
    }

    function openEdit(ev) {
        editingEvent = ev;
        resetValidation();

        const start = startOf(ev);
        let end = endOf(ev);

        // Tüm gün etkinliğinde hariç tutulan bitişi son güne çevir
        if (ev.isAllDay) {
            end = addDays(startOfDay(end), -1);
            if (end < startOfDay(start)) end = startOfDay(start);
        }

        els.eyebrow.textContent = "ETKİNLİĞİ DÜZENLE";
        els.heading.textContent = ev.title;
        els.title.value = ev.title;
        els.allDay.checked = ev.isAllDay;
        els.startDate.value = toDateInput(start);
        els.startTime.value = toTimeInput(start);
        els.endDate.value = toDateInput(end);
        els.endTime.value = toTimeInput(end);
        els.location.value = ev.location ?? "";
        els.description.value = ev.description ?? "";
        els.remove.hidden = false;

        syncAllDay();
        setBusy(false);
        modal.show();
    }

    function readForm() {
        const allDay = els.allDay.checked;

        let start;
        let end;

        if (allDay) {
            start = fromInputs(els.startDate.value);
            end = addDays(fromInputs(els.endDate.value), 1);
        } else {
            start = fromInputs(els.startDate.value, els.startTime.value);
            end = fromInputs(els.endDate.value, els.endTime.value);
        }

        return {
            title: els.title.value.trim(),
            description: els.description.value.trim() || null,
            location: els.location.value.trim() || null,
            isAllDay: allDay,
            startAtUtc: start.toISOString(),
            endAtUtc: end.toISOString(),
            calendarId: editingEvent?.calendarId ?? null,
            categoryId: editingEvent?.categoryId ?? null
        };
    }

    async function onSubmit(e) {
        e.preventDefault();
        resetValidation();

        els.title.value = els.title.value.trim();

        if (!els.form.checkValidity()) {
            els.form.classList.add("was-validated");
            return;
        }

        const body = readForm();

        if (new Date(body.endAtUtc) < new Date(body.startAtUtc)) {
            els.rangeError.hidden = false;
            return;
        }

        setBusy(true);

        try {
            const saved = editingEvent
                ? await api.update(editingEvent.id, body)
                : await api.create(body);

            modal.hide();
            onChange({ type: editingEvent ? "updated" : "created", event: saved });
        } catch (err) {
            showError(err.message);
        } finally {
            setBusy(false);
        }
    }

    async function onDelete() {
        if (!editingEvent) return;

        if (!window.confirm(`"${editingEvent.title}" etkinliği silinsin mi?`)) {
            return;
        }

        setBusy(true);

        try {
            await api.remove(editingEvent.id);
            modal.hide();
            onChange({ type: "deleted", event: editingEvent });
        } catch (err) {
            showError(err.message);
        } finally {
            setBusy(false);
        }
    }

    // ---------------------------------------------------------------
    // Ortak liste öğesi
    // ---------------------------------------------------------------

    function createEventItem(ev, onClick) {
        const item = document.createElement("button");
        item.type = "button";
        item.className = "event-item";
        item.style.setProperty("--event-color", colorOf(ev));

        const time = document.createElement("span");
        time.className = "event-item-time";
        time.textContent = formatTimeRange(ev);

        const title = document.createElement("span");
        title.className = "event-item-title";
        title.textContent = ev.title;

        item.append(time, title);

        if (ev.location) {
            const location = document.createElement("span");
            location.className = "event-item-location";
            location.textContent = ev.location;
            item.append(location);
        }

        item.addEventListener("click", () => onClick(ev));
        return item;
    }

    return {
        api,
        initModal,
        openCreate,
        openEdit,
        occursOn,
        formatTimeRange,
        colorOf,
        createEventItem,
        startOfDay,
        addDays,
        isSameDay
    };
})();
