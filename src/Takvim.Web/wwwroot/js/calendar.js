
document.addEventListener("DOMContentLoaded", () => {
    const Events = window.TakvimEvents;
    const MAX_CHIPS = 2;

    const monthTitle = document.getElementById("monthTitle");
    const calendarGrid = document.getElementById("calendarGrid");

    const selectedDayNumber =
        document.getElementById("selectedDayNumber");

    const selectedWeekday =
        document.getElementById("selectedWeekday");

    const selectedMonthYear =
        document.getElementById("selectedMonthYear");

    const dayEventList = document.getElementById("dayEventList");
    const dayEventsEmpty = document.getElementById("dayEventsEmpty");
    const dayEventsStatus = document.getElementById("dayEventsStatus");

    const today = new Date();

    let currentDate = new Date(
        today.getFullYear(),
        today.getMonth(),
        1
    );

    let selectedDate = new Date(
        today.getFullYear(),
        today.getMonth(),
        today.getDate()
    );

    // Görünen ayın (ve taşan günlerin) etkinlikleri
    let visibleEvents = [];
    let loadedRangeKey = null;
    let loadRequestId = 0;
    let loadError = null;

    const monthFormatter = new Intl.DateTimeFormat("tr-TR", {
        month: "long",
        year: "numeric"
    });

    const weekdayFormatter = new Intl.DateTimeFormat("tr-TR", {
        weekday: "long"
    });

    const monthYearFormatter = new Intl.DateTimeFormat("tr-TR", {
        month: "long",
        year: "numeric"
    });

    const fullDateFormatter = new Intl.DateTimeFormat("tr-TR", {
        day: "numeric",
        month: "long",
        year: "numeric",
        weekday: "long"
    });

    const isSameDay = Events.isSameDay;

    function eventsOn(date) {
        return visibleEvents.filter(ev => Events.occursOn(ev, date));
    }

    // Takvim ızgarasının kapsadığı tarih aralığı
    function gridRange() {
        const year = currentDate.getFullYear();
        const month = currentDate.getMonth();

        const firstDay = new Date(year, month, 1);
        const daysInMonth = new Date(year, month + 1, 0).getDate();
        const startOffset = (firstDay.getDay() + 6) % 7;
        const totalCells = Math.ceil((startOffset + daysInMonth) / 7) * 7;

        const start = new Date(year, month, 1 - startOffset);
        const end = Events.addDays(start, totalCells);

        return { start, end, startOffset, totalCells };
    }

    async function loadEvents(force = false) {
        const { start, end } = gridRange();
        const key = `${start.toISOString()}|${end.toISOString()}`;

        if (!force && key === loadedRangeKey) return;

        const requestId = ++loadRequestId;
        setStatus("Etkinlikler yükleniyor…");

        try {
            const events = await Events.api.list(start, end);

            // Bu arada başka bir ay açıldıysa eski yanıtı yok say
            if (requestId !== loadRequestId) return;

            visibleEvents = events ?? [];
            loadedRangeKey = key;
            loadError = null;
        } catch (err) {
            if (requestId !== loadRequestId) return;

            visibleEvents = [];
            loadedRangeKey = null;
            loadError = err.message;
        }

        setStatus(loadError);
        renderCalendar();
        renderDayEvents();
    }

    function setStatus(message) {
        dayEventsStatus.textContent = message ?? "";
        dayEventsStatus.hidden = !message;
        dayEventsStatus.classList.toggle("is-error", Boolean(loadError));
    }

    function updateSelectedDatePanel() {
        selectedDayNumber.textContent =
            selectedDate.getDate();

        selectedWeekday.textContent =
            weekdayFormatter.format(selectedDate);

        selectedMonthYear.textContent =
            monthYearFormatter.format(selectedDate);

        renderDayEvents();
    }

    function renderDayEvents() {
        dayEventList.replaceChildren();

        if (loadError) {
            dayEventsEmpty.hidden = true;
            return;
        }

        const events = eventsOn(selectedDate);

        for (const ev of events) {
            dayEventList.appendChild(
                Events.createEventItem(ev, Events.openEdit));
        }

        dayEventsEmpty.hidden = events.length > 0 || loadedRangeKey === null;
    }

    function selectDate(date) {
        const monthChanged =
            date.getFullYear() !== currentDate.getFullYear() ||
            date.getMonth() !== currentDate.getMonth();

        selectedDate = new Date(
            date.getFullYear(),
            date.getMonth(),
            date.getDate()
        );

        currentDate = new Date(
            date.getFullYear(),
            date.getMonth(),
            1
        );

        updateSelectedDatePanel();
        renderCalendar();

        if (monthChanged) loadEvents();
    }

    function createDayButton(date, isCurrentMonth) {
        const button = document.createElement("button");
        const events = eventsOn(date);

        button.type = "button";
        button.className = "calendar-day";

        const number = document.createElement("span");
        number.className = "day-number";
        number.textContent = date.getDate();
        button.appendChild(number);

        let label = fullDateFormatter.format(date);
        if (events.length) label += `, ${events.length} etkinlik`;
        button.setAttribute("aria-label", label);

        if (events.length) {
            button.classList.add("has-events");

            const list = document.createElement("span");
            list.className = "day-events";
            list.setAttribute("aria-hidden", "true");

            for (const ev of events.slice(0, MAX_CHIPS)) {
                const chip = document.createElement("span");
                chip.className = "day-chip";
                chip.style.setProperty("--event-color", Events.colorOf(ev));
                chip.textContent = ev.title;
                list.appendChild(chip);
            }

            if (events.length > MAX_CHIPS) {
                const more = document.createElement("span");
                more.className = "day-more";
                more.textContent = `+${events.length - MAX_CHIPS} daha`;
                list.appendChild(more);
            }

            // Dar ekranlarda başlıklar yerine nokta gösterilir
            const dot = document.createElement("span");
            dot.className = "day-dot";
            dot.style.setProperty("--event-color", Events.colorOf(events[0]));
            dot.setAttribute("aria-hidden", "true");

            button.append(list, dot);
        }

        if (!isCurrentMonth) {
            button.classList.add("other-month");
        }

        if (isSameDay(date, today)) {
            button.classList.add("today");
        }

        if (isSameDay(date, selectedDate)) {
            button.classList.add("selected");
            button.setAttribute("aria-pressed", "true");
        } else {
            button.setAttribute("aria-pressed", "false");
        }

        button.addEventListener("click", () => {
            selectDate(date);
        });

        return button;
    }

    function renderCalendar() {
        calendarGrid.innerHTML = "";

        const year = currentDate.getFullYear();
        const month = currentDate.getMonth();

        monthTitle.textContent =
            monthFormatter.format(currentDate);

        const { start, totalCells } = gridRange();

        for (let i = 0; i < totalCells; i++) {
            const date = Events.addDays(start, i);

            const isCurrentMonth =
                date.getFullYear() === year &&
                date.getMonth() === month;

            calendarGrid.appendChild(
                createDayButton(date, isCurrentMonth)
            );
        }
    }

    function changeMonth(delta) {
        currentDate = new Date(
            currentDate.getFullYear(),
            currentDate.getMonth() + delta,
            1
        );

        renderCalendar();
        loadEvents();
    }

    document.getElementById("prevMonth")
        .addEventListener("click", () => changeMonth(-1));

    document.getElementById("nextMonth")
        .addEventListener("click", () => changeMonth(1));

    document.getElementById("todayBtn")
        .addEventListener("click", () => {
            selectDate(today);
        });

    document.getElementById("addEventBtn")
        .addEventListener("click", () => Events.openCreate(selectedDate));

    document.getElementById("addDayEventBtn")
        .addEventListener("click", () => Events.openCreate(selectedDate));

    // Ekleme/güncelleme/silme sonrası takvimi yenile
    Events.initModal(({ type, event }) => {
        if (type !== "deleted" && event) {
            const start = new Date(event.startAtUtc);
            if (!Events.occursOn(event, selectedDate)) {
                selectDate(start);
            }
        }

        loadEvents(true);
    });

    updateSelectedDatePanel();
    renderCalendar();
    loadEvents();
});
