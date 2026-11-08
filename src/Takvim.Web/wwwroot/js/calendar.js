
document.addEventListener("DOMContentLoaded", () => {
    const monthTitle = document.getElementById("monthTitle");
    const calendarGrid = document.getElementById("calendarGrid");
    const selectedDateText = document.getElementById("selectedDate");

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

    const dateFormatter = new Intl.DateTimeFormat("tr-TR", {
        day: "numeric",
        month: "long",
        year: "numeric",
        weekday: "long"
    });

    function isSameDay(date1, date2) {
        return date1.getFullYear() === date2.getFullYear() &&
               date1.getMonth() === date2.getMonth() &&
               date1.getDate() === date2.getDate();
    }

    function selectDate(date) {
        selectedDate = new Date(
            date.getFullYear(),
            date.getMonth(),
            date.getDate()
        );

        selectedDateText.textContent =
            dateFormatter.format(selectedDate);

        renderCalendar();
    }

    function createDayButton(date, isCurrentMonth) {
        const button = document.createElement("button");

        button.type = "button";
        button.className = "calendar-day";
        button.textContent = date.getDate();

        button.setAttribute(
            "aria-label",
            dateFormatter.format(date)
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
        const daysInMonth = new Date(year, month + 1, 0)
            .getDate();

        // Haftanın ilk günü Pazartesi.
        const startOffset = (firstDay.getDay() + 6) % 7;

        const totalCells = Math.ceil(
            (startOffset + daysInMonth) / 7
        ) * 7;

        for (let i = 0; i < totalCells; i++) {
            const date = new Date(
                year,
                month,
                i - startOffset + 1
            );

            const isCurrentMonth =
                date.getMonth() === month &&
                date.getFullYear() === year;

            calendarGrid.appendChild(
                createDayButton(date, isCurrentMonth)
            );
        }
    }

    document.getElementById("prevMonth")
        .addEventListener("click", () => {
            currentDate.setMonth(currentDate.getMonth() - 1);
            renderCalendar();
        });

    document.getElementById("nextMonth")
        .addEventListener("click", () => {
            currentDate.setMonth(currentDate.getMonth() + 1);
            renderCalendar();
        });

    document.getElementById("todayBtn")
        .addEventListener("click", () => {
            currentDate = new Date(
                today.getFullYear(),
                today.getMonth(),
                1
            );

            selectDate(today);
        });

    selectedDateText.textContent =
        dateFormatter.format(selectedDate);

    renderCalendar();
});
