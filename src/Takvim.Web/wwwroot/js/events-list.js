/* Takvim+ — Etkinliklerim (liste görünümü) */
document.addEventListener("DOMContentLoaded", () => {
    const Events = window.TakvimEvents;

    const groupsEl = document.getElementById("eventsGroups");
    const emptyEl = document.getElementById("eventsEmpty");
    const statusEl = document.getElementById("eventsStatus");
    const countEl = document.getElementById("eventsCount");
    const filterButtons = document.querySelectorAll(".events-filter-btn");

    let filter = "upcoming";
    let requestId = 0;

    const groupFormatter = new Intl.DateTimeFormat("tr-TR", {
        weekday: "long",
        day: "numeric",
        month: "long",
        year: "numeric"
    });

    function setStatus(message, isError = false) {
        statusEl.textContent = message ?? "";
        statusEl.hidden = !message;
        statusEl.classList.toggle("is-error", isError);
    }

    async function load() {
        const current = ++requestId;
        const now = new Date();

        setStatus("Etkinlikler yükleniyor…");

        try {
            let events;

            if (filter === "upcoming") {
                events = await Events.api.list(now, null);
            } else if (filter === "past") {
                events = await Events.api.list(null, now);
                // Geçmiş etkinlikler: yalnızca bitmiş olanlar, en yeni üstte
                events = events
                    .filter(ev => new Date(ev.endAtUtc) <= now)
                    .reverse();
            } else {
                events = await Events.api.list(null, null);
            }

            if (current !== requestId) return;

            setStatus(null);
            render(events ?? []);
        } catch (err) {
            if (current !== requestId) return;

            groupsEl.replaceChildren();
            emptyEl.hidden = true;
            countEl.textContent = "Etkinlikler";
            setStatus(err.message, true);
        }
    }

    function render(events) {
        groupsEl.replaceChildren();
        countEl.textContent = `${events.length} etkinlik`;
        emptyEl.hidden = events.length > 0;

        // Başlangıç gününe göre grupla
        const groups = new Map();

        for (const ev of events) {
            const day = Events.startOfDay(new Date(ev.startAtUtc));
            const key = day.getTime();

            if (!groups.has(key)) groups.set(key, { day, items: [] });
            groups.get(key).items.push(ev);
        }

        for (const { day, items } of groups.values()) {
            const section = document.createElement("section");
            section.className = "events-group";

            const heading = document.createElement("h3");
            heading.className = "events-group-title";
            heading.textContent = groupFormatter.format(day);

            if (Events.isSameDay(day, new Date())) {
                const badge = document.createElement("span");
                badge.className = "today-badge";
                badge.textContent = "Bugün";
                heading.append(" ", badge);
            }

            const list = document.createElement("div");
            list.className = "day-event-list";

            for (const ev of items) {
                const item = Events.createEventItem(ev, Events.openEdit);

                if (ev.description) {
                    const desc = document.createElement("span");
                    desc.className = "event-item-desc";
                    desc.textContent = ev.description;
                    item.append(desc);
                }

                list.append(item);
            }

            section.append(heading, list);
            groupsEl.append(section);
        }
    }

    for (const button of filterButtons) {
        button.addEventListener("click", () => {
            filter = button.dataset.filter;

            for (const b of filterButtons) {
                const active = b === button;
                b.classList.toggle("active", active);
                b.setAttribute("aria-selected", String(active));
            }

            load();
        });
    }

    document.getElementById("addEventBtn")
        .addEventListener("click", () => Events.openCreate(new Date()));

    Events.initModal(() => load());

    load();
});
