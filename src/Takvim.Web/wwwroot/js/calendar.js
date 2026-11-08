
document.addEventListener("DOMContentLoaded", () => {
    const monthTitle = document.getElementById("monthTitle");
    const calendarGrid = document.getElementById("calendarGrid");

    const selectedDayNumber =
        document.getElementById("selectedDayNumber");

    const selectedWeekday =
        document.getElementById("selectedWeekday");

    const selectedMonthYear =
        document.getElementById("selectedMonthYear");

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

    function isSameDay(a, b) {
        return a.getFullYear() === b.getFullYear() &&
               a.getMonth() === b.getMonth() &&
               a.getDate() === b.getDate();
    }

    function updateSelectedDatePanel() {
        selectedDayNumber.textContent =
            selectedDate.getDate();

        selectedWeekday.textContent =
            weekdayFormatter.format(selectedDate);

        selectedMonthYear.textContent =
            monthYearFormatter.format(selectedDate);
    }

    function selectDate(date) {
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
    }

    function createDayButton(date, isCurrentMonth) {
        const button = document.createElement("button");

        button.type = "button";
        button.className = "calendar-day";
        button.textContent = date.getDate();

        button.setAttribute(
            "aria-label",
            fullDateFormatter.format(date)
        );

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

        const firstDay = new Date(year, month, 1);

        const daysInMonth =
            new Date(year, month + 1, 0).getDate();

        const startOffset =
            (firstDay.getDay() + 6) % 7;

        const totalCells =
            Math.ceil((startOffset + daysInMonth) / 7) * 7;

        for (let i = 0; i < totalCells; i++) {
            const date = new Date(
                year,
                month,
                i - startOffset + 1
            );

            const isCurrentMonth =
                date.getFullYear() === year &&
                date.getMonth() === month;

            calendarGrid.appendChild(
                createDayButton(date, isCurrentMonth)
            );
        }
    }

    document.getElementById("prevMonth")
        .addEventListener("click", () => {
            currentDate = new Date(
                currentDate.getFullYear(),
                currentDate.getMonth() - 1,
                1
            );

            renderCalendar();
        });

    document.getElementById("nextMonth")
        .addEventListener("click", () => {
            currentDate = new Date(
                currentDate.getFullYear(),
                currentDate.getMonth() + 1,
                1
            );

            renderCalendar();
        });

    document.getElementById("todayBtn")
        .addEventListener("click", () => {
            selectDate(today);
        });

    updateSelectedDatePanel();
    renderCalendar();
});
