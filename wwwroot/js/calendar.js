// État global du calendrier
let calendarState = {
    currentYear: new Date().getFullYear(),
    currentMonth: new Date().getMonth() + 1,
    events: []
};

function loadCalendarStateFromUrl() {
    const params = new URLSearchParams(window.location.search);
    const yearParam = params.get('year');
    const monthParam = params.get('month');

    if (yearParam) {
        const parsedYear = Number(yearParam);
        if (!Number.isNaN(parsedYear) && parsedYear > 0) {
            calendarState.currentYear = parsedYear;
        }
    }

    if (monthParam) {
        const parsedMonth = Number(monthParam);
        if (!Number.isNaN(parsedMonth) && parsedMonth >= 1 && parsedMonth <= 12) {
            calendarState.currentMonth = parsedMonth;
        }
    }
}

function loadEventsFromStorage() {
    const stored = localStorage.getItem('calendarEvents');
    if (stored) {
        calendarState.events = JSON.parse(stored);
    }
}

function saveEventsToStorage() {
    localStorage.setItem('calendarEvents', JSON.stringify(calendarState.events));
}

function goToMonth(year, month) {
    calendarState.currentYear = year;
    calendarState.currentMonth = month;
    window.location = `/?year=${year}&month=${month}`;
}

function goToPreviousMonth() {
    let newMonth = calendarState.currentMonth - 1;
    let newYear = calendarState.currentYear;

    if (newMonth < 1) {
        newMonth = 12;
        newYear--;
    }

    goToMonth(newYear, newMonth);
}

function goToNextMonth() {
    let newMonth = calendarState.currentMonth + 1;
    let newYear = calendarState.currentYear;

    if (newMonth > 12) {
        newMonth = 1;
        newYear++;
    }

    goToMonth(newYear, newMonth);
}

function goToToday() {
    const today = new Date();
    goToMonth(today.getFullYear(), today.getMonth() + 1);
}

function openEventForm(dateString) {
    const reservationUrl = `/Hotel/CreateReservation?date=${encodeURIComponent(dateString)}`;
    window.location.href = reservationUrl;
}

function closeEventForm() {
    const modal = document.getElementById('eventFormModal');
    if (!modal) return;
    modal.classList.remove('show');
    modal.style.display = 'none';
    document.body.classList.remove('modal-open');
}

function saveEvent(event) {
    event.preventDefault();

    const form = document.getElementById('eventForm');
    if (!form) return;

    const formData = new FormData(form);
    const eventData = {
        title: formData.get('title'),
        description: formData.get('description'),
        date: formData.get('date'),
        time: formData.get('time'),
        durationMinutes: parseInt(formData.get('durationMinutes')) || 60
    };

    if (!eventData.title || !eventData.title.trim()) {
        alert('Le titre est requis');
        return;
    }

    if (!eventData.date) {
        alert('La date est requise');
        return;
    }

    const dateTime = new Date(eventData.date + 'T' + (eventData.time || '00:00'));
    const newEvent = {
        id: Date.now(),
        title: eventData.title,
        description: eventData.description,
        start: dateTime.toISOString(),
        end: new Date(dateTime.getTime() + eventData.durationMinutes * 60000).toISOString()
    };

    calendarState.events.push(newEvent);
    saveEventsToStorage();
    closeEventForm();
    window.location.reload();
}

function deleteEvent(eventId) {
    if (confirm('Êtes-vous sûr de vouloir supprimer cet événement ?')) {
        calendarState.events = calendarState.events.filter(e => e.id != eventId);
        saveEventsToStorage();
        window.location.reload();
    }
}

function editEvent(eventId) {
    const event = calendarState.events.find(e => e.id == eventId);
    if (event) {
        const startDate = new Date(event.start);
        const hours = String(startDate.getHours()).padStart(2, '0');
        const minutes = String(startDate.getMinutes()).padStart(2, '0');

        const eventTitle = document.getElementById('eventTitle');
        const eventDescription = document.getElementById('eventDescription');
        const eventDate = document.getElementById('eventDate');
        const eventTime = document.getElementById('eventTime');
        const eventForm = document.getElementById('eventForm');
        const eventFormModal = document.getElementById('eventFormModal');

        if (eventTitle) eventTitle.value = event.title;
        if (eventDescription) eventDescription.value = event.description;
        if (eventDate) eventDate.value = startDate.toISOString().split('T')[0];
        if (eventTime) eventTime.value = `${hours}:${minutes}`;
        if (eventForm) eventForm.dataset.editId = eventId;
        if (eventFormModal) {
            eventFormModal.classList.add('show');
            eventFormModal.style.display = 'block';
        }
        document.body.classList.add('modal-open');
    }
}

document.addEventListener('DOMContentLoaded', function() {
    loadCalendarStateFromUrl();
    loadEventsFromStorage();

    document.querySelectorAll('.btn-prev-month').forEach(btn => {
        btn.addEventListener('click', goToPreviousMonth);
    });

    document.querySelectorAll('.btn-next-month').forEach(btn => {
        btn.addEventListener('click', goToNextMonth);
    });

    document.querySelectorAll('.btn-today').forEach(btn => {
        btn.addEventListener('click', goToToday);
    });

    const modal = document.getElementById('eventFormModal');
    if (modal) {
        modal.addEventListener('click', function(e) {
            if (e.target === this) {
                closeEventForm();
            }
        });
    }

    // Date cards are handled by the page-specific modal logic in Calendar.cshtml.
    // Keeping this file free of a redirect handler avoids conflicts with the
    // reservation workflow: click a day to open the modal, then either create a
    // reservation or review the bookings for that date.
});

document.addEventListener('keydown', function(e) {
    if (e.key === 'Escape') {
        closeEventForm();
    }
});
