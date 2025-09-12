/*!
 * Persian Date Picker for ASP.NET Core - Server-Side Conversion
 * Uses C# backend for accurate date conversions
 * Version: 3.0.0 - Server-side conversion
 */

(function (window, document) {
    'use strict';

    class PersianDatePicker {
        constructor() {
            this.persianMonths = [
                'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
                'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
            ];

            this.persianWeekdays = ['ش', 'ی', 'د', 'س', 'چ', 'پ', 'ج'];
            this.currentDate = null;
            this.activePickers = new Map();

            // Initialize
            this.loadTodayFromServer().then(() => {
                this.addCSS();
            });
        }

        async loadTodayFromServer() {
            try {
                const response = await fetch('/Base/GetTodayPersian');
                const result = await response.json();

                if (result.success) {
                    this.currentDate = {
                        year: result.year,
                        month: result.month,
                        day: result.day
                    };
                    console.log('Today Persian Date from server:', this.currentDate);
                } else {
                    // Fallback to approximate date
                    this.currentDate = { year: 1403, month: 6, day: 8 };
                    console.warn('Could not load today from server, using fallback');
                }
            } catch (error) {
                // Fallback to approximate date
                this.currentDate = { year: 1403, month: 6, day: 8 };
                console.warn('Error loading today from server:', error);
            }
        }

        async convertPersianToGregorian(persianDateString) {
            try {
                const response = await fetch('/Base/ConvertPersianDate', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                    },
                    body: JSON.stringify({ persianDate: persianDateString })
                });

                const result = await response.json();

                if (result.success) {
                    return result.gregorianDate;
                } else {
                    console.error('Server conversion error:', result.error);
                    return null;
                }
            } catch (error) {
                console.error('Error calling conversion API:', error);
                return null;
            }
        }

        toEnglishNumbers(str) {
            const persianDigits = '۰۱۲۳۴۵۶۷۸۹';
            const englishDigits = '0123456789';
            return str.replace(/[۰-۹]/g, digit => englishDigits[persianDigits.indexOf(digit)]);
        }

        toPersianNumber(num) {
            const persianDigits = '۰۱۲۳۴۵۶۷۸۹';
            return num.toString().replace(/\d/g, digit => persianDigits[digit]);
        }

        parseAndValidateInput(value) {
            value = this.toEnglishNumbers(value);
            const pattern = /^(\d{4})\/(\d{1,2})\/(\d{1,2})$/;
            const match = value.match(pattern);

            if (!match) return null;

            const year = parseInt(match[1]);
            const month = parseInt(match[2]);
            const day = parseInt(match[3]);

            // Basic validation
            if (year < 1200 || year > 1600) return null;
            if (month < 1 || month > 12) return null;
            if (day < 1 || day > 31) return null;

            return { year, month, day };
        }

        getDaysInMonth(year, month) {
            // Simplified - server will handle exact validation
            if (month <= 6) return 31;
            if (month <= 11) return 30;
            return 29; // Esfand - could be 29 or 30
        }

        addCSS() {
            if (document.getElementById('persian-datepicker-css')) return;

            const css = `
                .persian-date-wrapper {
                    position: relative;
                }
                
                .persian-date-input {
                    cursor: pointer !important;
                    text-align: center;
                    direction: ltr;
                    padding-left: 70px !important;
                }
                
                .persian-date-buttons {
                    position: absolute;
                    left: 5px;
                    top: 50%;
                    transform: translateY(-50%);
                    display: flex;
                    gap: 2px;
                    z-index: 3;
                    pointer-events: none;
                }
                
                .persian-date-buttons button {
                    pointer-events: auto;
                    background: transparent;
                    border: none;
                    padding: 2px 4px;
                    font-size: 14px;
                    cursor: pointer;
                    border-radius: 3px;
                }
                
                .persian-date-buttons button:hover {
                    background: rgba(0,0,0,0.1);
                }
                
                .persian-datepicker {
                    position: absolute;
                    top: 100%;
                    left: 0;
                    right: 0;
                    z-index: 1050;
                    margin-top: 2px;
                    display: none;
                    direction: rtl;
                }
                
                .persian-datepicker.show {
                    display: block;
                }
                
                .persian-datepicker-header {
                    display: flex;
                    justify-content: space-between;
                    align-items: center;
                    margin-bottom: 1rem;
                }
                
                .persian-month-year {
                    font-weight: 600;
                    font-size: 1rem;
                }
                
                .persian-weekdays {
                    display: grid;
                    grid-template-columns: repeat(7, 1fr);
                    gap: 2px;
                    margin-bottom: 0.5rem;
                }
                
                .persian-weekday {
                    text-align: center;
                    font-size: 0.75rem;
                    font-weight: 600;
                    padding: 0.25rem;
                }
                
                .persian-days-grid {
                    display: grid;
                    grid-template-columns: repeat(7, 1fr);
                    gap: 2px;
                }
                
                .persian-day {
                    aspect-ratio: 1;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    cursor: pointer;
                    font-size: 0.875rem;
                    position: relative;
                    min-height: 32px;
                }
            `;

            const style = document.createElement('style');
            style.id = 'persian-datepicker-css';
            style.textContent = css;
            document.head.appendChild(style);
        }

        initializeDatePicker(inputElement) {
            if (this.activePickers.has(inputElement)) return;

            // Find existing hidden field
            let hiddenField = null;
            const parentDiv = inputElement.closest('.mb-3') || inputElement.parentElement;
            if (parentDiv) {
                hiddenField = parentDiv.querySelector('input[type="hidden"].persian-date-hidden');
            }

            const wrapper = document.createElement('div');
            wrapper.className = 'persian-date-wrapper';

            inputElement.parentNode.insertBefore(wrapper, inputElement);
            wrapper.appendChild(inputElement);

            const originalName = inputElement.getAttribute('name');
            inputElement.removeAttribute('name');

            if (hiddenField && originalName) {
                hiddenField.setAttribute('name', originalName);
            }

            inputElement.classList.add('persian-date-input');
            inputElement.setAttribute('placeholder', '1403/06/08');
            inputElement.setAttribute('maxlength', '10');
            inputElement.setAttribute('autocomplete', 'off');

            // Create buttons container inside text box
            const buttonsContainer = document.createElement('div');
            buttonsContainer.className = 'persian-date-buttons';

            // Create calendar button
            const calendarButton = document.createElement('button');
            calendarButton.type = 'button';
            calendarButton.innerHTML = '📅';
            calendarButton.title = 'انتخاب تاریخ';
            buttonsContainer.appendChild(calendarButton);

            // Create toggle button
            const toggleButton = document.createElement('button');
            toggleButton.type = 'button';
            toggleButton.innerHTML = '🔄';
            toggleButton.title = 'تغییر نوع تقویم (شمسی/میلادی)';
            buttonsContainer.appendChild(toggleButton);

            wrapper.appendChild(buttonsContainer);

            // Create datepicker
            const datepicker = this.createDatepicker();
            wrapper.appendChild(datepicker);

            // Initialize picker state
            const pickerState = {
                inputElement,
                datepicker,
                wrapper,
                hiddenField,
                originalName,
                selectedDate: null,
                viewYear: this.currentDate ? this.currentDate.year : 1403,
                viewMonth: this.currentDate ? this.currentDate.month : 6,
                showGregorian: false, // Track display mode
                calendarType: 'persian', // Track calendar type: 'persian' or 'gregorian'
                calendarButton,
                toggleButton
            };

            this.activePickers.set(inputElement, pickerState);

            this.loadExistingValue(pickerState);
            this.setupEventListeners(pickerState);
            this.updateCalendar(pickerState);

            if (!pickerState.selectedDate && this.currentDate) {
                this.selectDate(pickerState, this.currentDate.year, this.currentDate.month, this.currentDate.day);
            }

            return pickerState;
        }

        loadExistingValue(pickerState) {
            // Try to load from hidden field first (database value)
            if (pickerState.hiddenField && pickerState.hiddenField.value) {
                try {
                    const date = new Date(pickerState.hiddenField.value);
                    if (!isNaN(date.getTime())) {
                        // We would need server-side conversion from Gregorian to Persian
                        // For now, just mark that we have a value
                        console.log('Loaded existing date from hidden field:', pickerState.hiddenField.value);
                        return;
                    }
                } catch (e) {
                    console.warn('Error parsing date from hidden field:', e);
                }
            }

            // Load from input value
            if (pickerState.inputElement.value) {
                const parsed = this.parseAndValidateInput(pickerState.inputElement.value);
                if (parsed) {
                    pickerState.selectedDate = parsed;
                    pickerState.viewYear = parsed.year;
                    pickerState.viewMonth = parsed.month;
                }
            }
        }

        createDatepicker() {
            const datepicker = document.createElement('div');
            datepicker.className = 'persian-datepicker';

            datepicker.innerHTML = `
                <div class="card shadow-sm">
                    <div class="card-body p-3">
                        <div class="persian-datepicker-header">
                            <button type="button" class="persian-nav-btn btn btn-sm next-month" style="border: none;">
                                ▶
                            </button>
                            <div class="persian-month-year text-center fw-semibold"></div>
                            <button type="button" class="persian-nav-btn btn btn-sm prev-month" style="border: none;">
                                ◀
                            </button>
                        </div>
                        
                        <div class="text-center mb-2">
                            <div class="btn-group btn-group-sm" role="group">
                                <button type="button" class="btn btn-outline-primary calendar-type-btn persian-calendar active" data-type="persian">
                                    شمسی
                                </button>
                                <button type="button" class="btn btn-outline-primary calendar-type-btn gregorian-calendar" data-type="gregorian">
                                    میلادی
                                </button>
                            </div>
                        </div>
                        
                        <div class="persian-weekdays">
                            <div class="persian-weekday text-muted text-center small fw-semibold">ش</div>
                            <div class="persian-weekday text-muted text-center small fw-semibold">ی</div>
                            <div class="persian-weekday text-muted text-center small fw-semibold">د</div>
                            <div class="persian-weekday text-muted text-center small fw-semibold">س</div>
                            <div class="persian-weekday text-muted text-center small fw-semibold">چ</div>
                            <div class="persian-weekday text-muted text-center small fw-semibold">پ</div>
                            <div class="persian-weekday text-muted text-center small fw-semibold">ج</div>
                        </div>
                        
                        <div class="persian-days-grid mb-3"></div>
                        
                        <button type="button" class="persian-today-btn btn btn-outline-secondary btn-sm w-100">
                            امروز
                        </button>
                    </div>
                </div>
            `;

            return datepicker;
        }

        setupEventListeners(pickerState) {
            const { inputElement, datepicker, wrapper, calendarButton, toggleButton } = pickerState;

            // Input click
            inputElement.addEventListener('click', (e) => {
                e.preventDefault();
                this.toggleDatepicker(pickerState);
            });

            // Calendar button click
            if (calendarButton) {
                calendarButton.addEventListener('click', (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    this.toggleDatepicker(pickerState);
                });
            }

            // Toggle button click (Persian/Gregorian display switch)
            if (toggleButton) {
                toggleButton.addEventListener('click', (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    this.toggleDateFormat(pickerState);
                });
            }

            // Calendar type buttons (Persian/Gregorian calendar switch)
            const calendarTypeButtons = datepicker.querySelectorAll('.calendar-type-btn');
            calendarTypeButtons.forEach(btn => {
                btn.addEventListener('click', (e) => {
                    e.preventDefault();
                    this.switchCalendarType(pickerState, e.target.dataset.type);
                });
            });

            // Navigation buttons
            const prevBtn = datepicker.querySelector('.prev-month');
            const nextBtn = datepicker.querySelector('.next-month');
            const todayBtn = datepicker.querySelector('.persian-today-btn');

            if (prevBtn) {
                prevBtn.addEventListener('click', (e) => {
                    e.preventDefault();
                    this.previousMonth(pickerState);
                });
            }

            if (nextBtn) {
                nextBtn.addEventListener('click', (e) => {
                    e.preventDefault();
                    this.nextMonth(pickerState);
                });
            }

            if (todayBtn) {
                todayBtn.addEventListener('click', (e) => {
                    e.preventDefault();
                    this.goToToday(pickerState);
                });
            }
        }

        toggleDatepicker(pickerState) {
            this.activePickers.forEach((picker, input) => {
                if (picker !== pickerState) {
                    this.hideDatepicker(picker);
                }
            });

            pickerState.datepicker.classList.toggle('show');
        }

        hideDatepicker(pickerState) {
            pickerState.datepicker.classList.remove('show');
        }

        updateCalendar(pickerState) {
            const { datepicker, viewYear, viewMonth, calendarType } = pickerState;
            const monthYear = datepicker.querySelector('.persian-month-year');
            const daysGrid = datepicker.querySelector('.persian-days-grid');

            if (!monthYear || !daysGrid) {
                console.error('Calendar elements not found');
                return;
            }

            if (calendarType === 'gregorian') {
                // Gregorian calendar
                monthYear.textContent = `${this.getGregorianMonthName(viewMonth)} ${viewYear}`;
                daysGrid.innerHTML = '';

                const daysInMonth = this.getGregorianDaysInMonth(viewYear, viewMonth);
                const firstDay = new Date(viewYear, viewMonth - 1, 1).getDay(); // 0 = Sunday

                // Create calendar grid
                for (let i = 0; i < 42; i++) {
                    let dayEl;
                    const dayNumber = i - firstDay + 1;

                    if (dayNumber < 1) {
                        // Previous month
                        const prevMonth = viewMonth === 1 ? 12 : viewMonth - 1;
                        const prevYear = viewMonth === 1 ? viewYear - 1 : viewYear;
                        const prevMonthDays = this.getGregorianDaysInMonth(prevYear, prevMonth);
                        dayEl = this.createDayElement(prevMonthDays + dayNumber, 'other-month');
                    } else if (dayNumber > daysInMonth) {
                        // Next month
                        dayEl = this.createDayElement(dayNumber - daysInMonth, 'other-month');
                    } else {
                        // Current month
                        dayEl = this.createDayElement(dayNumber, 'current-month');

                        // Mark today (Gregorian)
                        const today = new Date();
                        if (viewYear === today.getFullYear() &&
                            viewMonth === (today.getMonth() + 1) &&
                            dayNumber === today.getDate()) {
                            dayEl.classList.remove('btn-outline-light');
                            dayEl.classList.add('btn-primary', 'today');
                        }

                        // Mark selected (check against original Gregorian date for accuracy)
                        if (pickerState.selectedDate && pickerState.originalGregorianDate) {
                            const { year: selYear, month: selMonth, day: selDay } = pickerState.originalGregorianDate;

                            if (viewYear === selYear && viewMonth === selMonth && dayNumber === selDay) {
                                dayEl.classList.remove('btn-outline-light', 'btn-primary');
                                dayEl.classList.add('btn-success', 'selected');

                                console.log(`Selected Gregorian date marked in calendar: ${selYear}/${selMonth}/${selDay}`);
                            }
                        }

                        // Add click handler
                        const currentDay = dayNumber;
                        dayEl.addEventListener('click', (e) => {
                            e.preventDefault();
                            this.selectGregorianDate(pickerState, viewYear, viewMonth, currentDay);
                        });
                    }

                    daysGrid.appendChild(dayEl);
                }
            } else {
                // Persian calendar (original logic)
                monthYear.textContent = `${this.persianMonths[viewMonth - 1]} ${this.toPersianNumber(viewYear)}`;
                daysGrid.innerHTML = '';

                const daysInMonth = this.getDaysInMonth(viewYear, viewMonth);

                // Simple grid - fill 42 cells (6 weeks × 7 days)
                for (let i = 0; i < 42; i++) {
                    let dayEl;
                    let day = i - 5; // Start a few days before month starts

                    if (day < 1) {
                        // Previous month days
                        const prevMonth = viewMonth === 1 ? 12 : viewMonth - 1;
                        const prevYear = viewMonth === 1 ? viewYear - 1 : viewYear;
                        const prevMonthDays = this.getDaysInMonth(prevYear, prevMonth);
                        dayEl = this.createDayElement(prevMonthDays + day, 'other-month');
                    } else if (day > daysInMonth) {
                        // Next month days
                        dayEl = this.createDayElement(day - daysInMonth, 'other-month');
                    } else {
                        // Current month days
                        dayEl = this.createDayElement(day, 'current-month');

                        // Mark today
                        if (this.currentDate && viewYear === this.currentDate.year &&
                            viewMonth === this.currentDate.month && day === this.currentDate.day) {
                            dayEl.classList.remove('btn-outline-light');
                            dayEl.classList.add('btn-primary', 'today');
                        }

                        // Mark selected
                        if (pickerState.selectedDate && viewYear === pickerState.selectedDate.year &&
                            viewMonth === pickerState.selectedDate.month && day === pickerState.selectedDate.day) {
                            dayEl.classList.remove('btn-outline-light', 'btn-primary');
                            dayEl.classList.add('btn-success', 'selected');
                        }

                        // Add click handler for current month days
                        const currentDay = day;
                        dayEl.addEventListener('click', (e) => {
                            e.preventDefault();
                            this.selectDate(pickerState, viewYear, viewMonth, currentDay);
                        });
                    }

                    daysGrid.appendChild(dayEl);
                }
            }
        }

        toggleDateFormat(pickerState) {
            if (!pickerState.selectedDate) {
                return; // No date selected, nothing to toggle
            }

            pickerState.showGregorian = !pickerState.showGregorian;
            this.updateDisplayInput(pickerState);

            // Close the datepicker when toggling
            this.hideDatepicker(pickerState);
        }

        getFirstDayOfMonth(year, month) {
            // Simple approximation - ideally should use server calculation
            // Saturday = 0, Sunday = 1, ... Friday = 6
            return Math.floor(Math.random() * 7); // Placeholder - should be calculated properly
        }

        createDayElement(day, type) {
            const dayEl = document.createElement('button');
            dayEl.type = 'button';

            // Base Bootstrap button classes
            dayEl.className = 'persian-day btn btn-sm btn-outline-light text-center';

            if (type === 'other-month') {
                dayEl.classList.add('text-muted');
            }

            dayEl.textContent = this.toPersianNumber(day);
            dayEl.style.minHeight = '32px';
            dayEl.style.aspectRatio = '1';

            return dayEl;
        }

        async selectDate(pickerState, year, month, day) {
            // Validate date first
            const isValidDate = this.validateDate(year, month, day, pickerState.calendarType);
            if (!isValidDate) {
                console.warn(`Invalid date: ${year}/${month}/${day} for ${pickerState.calendarType} calendar`);
                return;
            }

            pickerState.selectedDate = { year, month, day };

            // Update all displays and state
            await this.updateAllDisplays(pickerState);
            this.updateCalendar(pickerState);
            this.hideDatepicker(pickerState);
        }

        async selectGregorianDate(pickerState, year, month, day) {
            console.log(`Selecting Gregorian date: ${year}/${month}/${day}`);

            // Validate Gregorian date first
            const isValidDate = this.validateGregorianDate(year, month, day);
            if (!isValidDate) {
                console.warn(`Invalid Gregorian date: ${year}/${month}/${day}`);
                return;
            }

            // Convert Gregorian to Persian using server API
            try {
                const result = await this.convertGregorianToPersian(year, month, day);

                if (result && result.persianDate) {
                    console.log(`Gregorian ${year}/${month}/${day} -> Persian ${result.persianDate.year}/${result.persianDate.month}/${result.persianDate.day}`);
                    console.log(`Server returned DateTime: ${result.gregorianDate}`);

                    pickerState.selectedDate = result.persianDate;
                    pickerState.gregorianDate = result.gregorianDate;

                    await this.updateAllDisplays(pickerState);
                    this.updateCalendar(pickerState);
                    this.hideDatepicker(pickerState);
                } else {
                    console.error('Failed to convert Gregorian date');
                }
            } catch (error) {
                console.error('Error converting Gregorian date:', error);
            }
        }

        async convertGregorianToPersian(year, month, day) {
            try {
                console.log(`Converting Gregorian to Persian: ${year}/${month}/${day}`);

                const response = await fetch('/Base/ConvertGregorianDate', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                    },
                    body: JSON.stringify({ year: year, month: month, day: day })
                });

                const result = await response.json();

                if (result.success) {
                    console.log('Gregorian to Persian server response:', result);
                    return result;
                } else {
                    console.error('Server conversion error:', result.error);
                    return null;
                }
            } catch (error) {
                console.error('Error calling Gregorian conversion API:', error);
                return null;
            }
        }

        validateDate(year, month, day, calendarType = 'persian') {
            if (calendarType === 'persian') {
                // Persian date validation
                if (year < 1200 || year > 1600) return false;
                if (month < 1 || month > 12) return false;
                if (day < 1) return false;

                const maxDays = this.getDaysInMonth(year, month);
                return day <= maxDays;
            } else {
                return this.validateGregorianDate(year, month, day);
            }
        }

        validateGregorianDate(year, month, day) {
            if (year < 1900 || year > 2200) return false;
            if (month < 1 || month > 12) return false;
            if (day < 1) return false;

            const maxDays = this.getGregorianDaysInMonth(year, month);
            return day <= maxDays;
        }

        async updateAllDisplays(pickerState) {
            // Update display input
            this.updateDisplayInput(pickerState);

            // Update hidden field with server conversion
            await this.updateHiddenField(pickerState);

            // Update toggle button state
            this.updateToggleButton(pickerState);

            // Update placeholder if needed
            this.updateInputPlaceholder(pickerState);
        }

        updateInputPlaceholder(pickerState) {
            if (pickerState.calendarType === 'gregorian') {
                pickerState.inputElement.setAttribute('placeholder', '2024/10/29');
            } else {
                pickerState.inputElement.setAttribute('placeholder', '1403/06/08');
            }
        }

        toggleDateFormat(pickerState) {
            if (!pickerState.selectedDate) {
                return; // No date selected, nothing to toggle
            }

            pickerState.showGregorian = !pickerState.showGregorian;

            // Synchronize calendar type with display toggle
            if (pickerState.showGregorian) {
                pickerState.calendarType = 'gregorian';

                if (pickerState.gregorianDate) {
                    // Parse the gregorian date to set the view - use the same parsing logic as display
                    let gregDate;
                    const serverDateTime = pickerState.gregorianDate;

                    if (serverDateTime.includes('T')) {
                        // Full DateTime format from server
                        gregDate = new Date(serverDateTime);
                    } else {
                        // Simple date format
                        const parts = serverDateTime.split('/');
                        if (parts.length === 3) {
                            gregDate = new Date(parseInt(parts[0]), parseInt(parts[1]) - 1, parseInt(parts[2]));
                        }
                    }

                    if (gregDate && !isNaN(gregDate.getTime())) {
                        pickerState.viewYear = gregDate.getFullYear();
                        pickerState.viewMonth = gregDate.getMonth() + 1;

                        console.log(`Toggled to Gregorian: ${gregDate.getFullYear()}/${gregDate.getMonth() + 1}/${gregDate.getDate()}`);
                    }
                }
            } else {
                // Switch back to Persian calendar
                pickerState.calendarType = 'persian';
                if (pickerState.selectedDate) {
                    pickerState.viewYear = pickerState.selectedDate.year;
                    pickerState.viewMonth = pickerState.selectedDate.month;
                }

                console.log(`Toggled to Persian: ${pickerState.selectedDate.year}/${pickerState.selectedDate.month}/${pickerState.selectedDate.day}`);
            }

            // Update calendar type buttons to match toggle
            this.updateCalendarTypeButtons(pickerState);

            // Update calendar content based on new type
            this.updateCalendarContent(pickerState);

            // Update all displays
            this.updateDisplayInput(pickerState);
            this.updateToggleButton(pickerState);
            this.updateCalendar(pickerState);

            // Close the datepicker when toggling
            this.hideDatepicker(pickerState);
        }

        updateCalendarTypeButtons(pickerState) {
            const calendarTypeButtons = pickerState.datepicker.querySelectorAll('.calendar-type-btn');
            calendarTypeButtons.forEach(btn => {
                btn.classList.remove('active', 'btn-primary');
                btn.classList.add('btn-outline-primary');
            });

            const targetType = pickerState.calendarType;
            const activeBtn = pickerState.datepicker.querySelector(`[data-type="${targetType}"]`);
            if (activeBtn) {
                activeBtn.classList.remove('btn-outline-primary');
                activeBtn.classList.add('active', 'btn-primary');
            }
        }

        updateToggleButton(pickerState) {
            if (!pickerState.toggleButton) return;

            if (pickerState.showGregorian) {
                pickerState.toggleButton.innerHTML = '📆';
                pickerState.toggleButton.title = 'نمایش تاریخ شمسی';
                pickerState.toggleButton.style.color = '#fd7e14'; // Orange
            } else {
                pickerState.toggleButton.innerHTML = '🔄';
                pickerState.toggleButton.title = 'نمایش تاریخ میلادی';
                pickerState.toggleButton.style.color = '#0dcaf0'; // Info blue
            }
        }

        switchCalendarType(pickerState, type) {
            const oldType = pickerState.calendarType;
            pickerState.calendarType = type;

            // Synchronize display toggle with calendar type
            pickerState.showGregorian = (type === 'gregorian');

            console.log(`Switching calendar type from ${oldType} to ${type}`);

            // Update button states
            this.updateCalendarTypeButtons(pickerState);

            // Update weekdays and calendar content
            this.updateCalendarContent(pickerState);

            // Set appropriate view year/month based on type
            if (type === 'gregorian') {
                if (pickerState.selectedDate && pickerState.gregorianDate) {
                    // Use the server-provided Gregorian date for view
                    const parsedDate = new Date(pickerState.gregorianDate);
                    if (!isNaN(parsedDate.getTime())) {
                        pickerState.viewYear = parsedDate.getFullYear();
                        pickerState.viewMonth = parsedDate.getMonth() + 1;
                        console.log(`Gregorian view set to: ${pickerState.viewYear}/${pickerState.viewMonth}`);
                    }
                } else {
                    // Default to current Gregorian date
                    const now = new Date();
                    pickerState.viewYear = now.getFullYear();
                    pickerState.viewMonth = now.getMonth() + 1;
                }
            } else {
                // Persian calendar
                if (pickerState.selectedDate) {
                    pickerState.viewYear = pickerState.selectedDate.year;
                    pickerState.viewMonth = pickerState.selectedDate.month;
                    console.log(`Persian view set to: ${pickerState.viewYear}/${pickerState.viewMonth}`);
                } else if (this.currentDate) {
                    pickerState.viewYear = this.currentDate.year;
                    pickerState.viewMonth = this.currentDate.month;
                }
            }

            // Update input display and toggle button to match calendar type
            this.updateDisplayInput(pickerState);
            this.updateToggleButton(pickerState);

            // Always update calendar when switching types
            this.updateCalendar(pickerState);
        }

        updateDisplayInput(pickerState) {
            if (!pickerState.selectedDate) {
                pickerState.inputElement.value = '';
                return;
            }

            const { year, month, day } = pickerState.selectedDate;

            try {
                if (pickerState.showGregorian && pickerState.gregorianDate) {
                    // Parse Gregorian date directly from server DateTime string to avoid timezone issues
                    let formattedDate;
                    const serverDateTime = pickerState.gregorianDate;

                    if (serverDateTime.includes('T')) {
                        // Parse DateTime string manually to avoid timezone shifts
                        const datePart = serverDateTime.split('T')[0];
                        const dateComponents = datePart.split('-');

                        if (dateComponents.length === 3) {
                            const year = dateComponents[0];
                            const month = dateComponents[1];
                            const day = dateComponents[2];
                            formattedDate = `${year}/${month}/${day}`;
                        }
                    } else if (serverDateTime.includes('/')) {
                        // Already in the right format
                        formattedDate = serverDateTime;
                    }

                    if (formattedDate) {
                        pickerState.inputElement.value = formattedDate;
                        pickerState.inputElement.style.direction = 'ltr';
                        console.log(`Display Gregorian: ${formattedDate} (from server: ${serverDateTime})`);
                    }
                } else {
                    // Show Persian date
                    const yearStr = year.toString();
                    const monthStr = month.toString().padStart(2, '0');
                    const dayStr = day.toString().padStart(2, '0');

                    // Convert to Persian numbers
                    const yearPersian = this.toPersianNumber(yearStr);
                    const monthPersian = this.toPersianNumber(monthStr);
                    const dayPersian = this.toPersianNumber(dayStr);

                    const persianDate = `${yearPersian}/${monthPersian}/${dayPersian}`;
                    pickerState.inputElement.value = persianDate;
                    pickerState.inputElement.style.direction = 'ltr';

                    console.log(`Display Persian: ${persianDate}`);
                }

                // Trigger input event for any listeners
                pickerState.inputElement.dispatchEvent(new Event('input', { bubbles: true }));
                pickerState.inputElement.dispatchEvent(new Event('change', { bubbles: true }));

            } catch (error) {
                console.error('Error updating display input:', error);
                pickerState.inputElement.value = '';
            }
        }

        toggleDateFormat(pickerState) {
            if (!pickerState.selectedDate) {
                return; // No date selected, nothing to toggle
            }

            pickerState.showGregorian = !pickerState.showGregorian;

            // If we're switching to Gregorian display, also switch calendar to Gregorian
            if (pickerState.showGregorian && pickerState.gregorianDate) {
                // Parse the gregorian date to set the view - use the same parsing logic as display
                let gregDate;
                const serverDateTime = pickerState.gregorianDate;

                if (serverDateTime.includes('T')) {
                    // Full DateTime format from server
                    gregDate = new Date(serverDateTime);
                } else {
                    // Simple date format
                    const parts = serverDateTime.split('/');
                    if (parts.length === 3) {
                        gregDate = new Date(parseInt(parts[0]), parseInt(parts[1]) - 1, parseInt(parts[2]));
                    }
                }

                if (gregDate && !isNaN(gregDate.getTime())) {
                    pickerState.calendarType = 'gregorian';
                    pickerState.viewYear = gregDate.getFullYear();
                    pickerState.viewMonth = gregDate.getMonth() + 1;
                    this.switchCalendarType(pickerState, 'gregorian');

                    console.log(`Calendar switched to Gregorian: ${gregDate.getFullYear()}/${gregDate.getMonth() + 1}/${gregDate.getDate()}`);
                }
            } else {
                // Switch back to Persian calendar
                pickerState.calendarType = 'persian';
                if (pickerState.selectedDate) {
                    pickerState.viewYear = pickerState.selectedDate.year;
                    pickerState.viewMonth = pickerState.selectedDate.month;
                }
                this.switchCalendarType(pickerState, 'persian');

                console.log(`Calendar switched to Persian: ${pickerState.selectedDate.year}/${pickerState.selectedDate.month}/${pickerState.selectedDate.day}`);
            }

            // Update all displays to ensure consistency
            this.updateDisplayInput(pickerState);
            this.updateToggleButton(pickerState);

            // Update calendar to reflect the new view
            this.updateCalendar(pickerState);

            // Close the datepicker when toggling
            this.hideDatepicker(pickerState);
        }

        switchCalendarType(pickerState, type) {
            const oldType = pickerState.calendarType;
            pickerState.calendarType = type;

            // Update button states
            const calendarTypeButtons = pickerState.datepicker.querySelectorAll('.calendar-type-btn');
            calendarTypeButtons.forEach(btn => {
                btn.classList.remove('active', 'btn-primary');
                btn.classList.add('btn-outline-primary');
            });

            const activeBtn = pickerState.datepicker.querySelector(`[data-type="${type}"]`);
            if (activeBtn) {
                activeBtn.classList.remove('btn-outline-primary');
                activeBtn.classList.add('active', 'btn-primary');
            }

            // Update weekdays header
            const weekdaysContainer = pickerState.datepicker.querySelector('.persian-weekdays');
            if (type === 'gregorian') {
                // English weekdays (Sunday to Saturday)
                weekdaysContainer.innerHTML = `
                    <div class="persian-weekday text-muted text-center small fw-semibold">Sun</div>
                    <div class="persian-weekday text-muted text-center small fw-semibold">Mon</div>
                    <div class="persian-weekday text-muted text-center small fw-semibold">Tue</div>
                    <div class="persian-weekday text-muted text-center small fw-semibold">Wed</div>
                    <div class="persian-weekday text-muted text-center small fw-semibold">Thu</div>
                    <div class="persian-weekday text-muted text-center small fw-semibold">Fri</div>
                    <div class="persian-weekday text-muted text-center small fw-semibold">Sat</div>
                `;

                // If we have a selected date and Gregorian date, use it
                if (pickerState.selectedDate && pickerState.gregorianDate) {
                    const gregDate = new Date(pickerState.gregorianDate);
                    if (!isNaN(gregDate.getTime())) {
                        pickerState.viewYear = gregDate.getFullYear();
                        pickerState.viewMonth = gregDate.getMonth() + 1;
                    }
                } else {
                    // Default to current Gregorian date
                    const now = new Date();
                    pickerState.viewYear = now.getFullYear();
                    pickerState.viewMonth = now.getMonth() + 1;
                }
            } else {
                // Persian weekdays
                weekdaysContainer.innerHTML = `
                    <div class="persian-weekday text-muted text-center small fw-semibold">ش</div>
                    <div class="persian-weekday text-muted text-center small fw-semibold">ی</div>
                    <div class="persian-weekday text-muted text-center small fw-semibold">د</div>
                    <div class="persian-weekday text-muted text-center small fw-semibold">س</div>
                    <div class="persian-weekday text-muted text-center small fw-semibold">چ</div>
                    <div class="persian-weekday text-muted text-center small fw-semibold">پ</div>
                    <div class="persian-weekday text-muted text-center small fw-semibold">ج</div>
                `;

                // If we have a selected Persian date, use it
                if (pickerState.selectedDate) {
                    pickerState.viewYear = pickerState.selectedDate.year;
                    pickerState.viewMonth = pickerState.selectedDate.month;
                } else if (this.currentDate) {
                    // Default to current Persian date
                    pickerState.viewYear = this.currentDate.year;
                    pickerState.viewMonth = this.currentDate.month;
                }
            }

            // Only update calendar if type actually changed
            if (oldType !== type) {
                this.updateCalendar(pickerState);
            }
        }

        getGregorianMonthName(month) {
            const months = [
                'January', 'February', 'March', 'April', 'May', 'June',
                'July', 'August', 'September', 'October', 'November', 'December'
            ];
            return months[month - 1] || '';
        }

        getGregorianDaysInMonth(year, month) {
            return new Date(year, month, 0).getDate();
        }

        async selectGregorianDate(pickerState, year, month, day) {
            // Convert Gregorian to Persian using server API
            try {
                const gregorianDateString = `${year}/${month.toString().padStart(2, '0')}/${day.toString().padStart(2, '0')}`;

                // For now, we'll use a simple conversion (this should ideally use a server API)
                // You might want to add a server endpoint for Gregorian to Persian conversion
                const persianDate = await this.convertGregorianToPersian(year, month, day);

                if (persianDate) {
                    pickerState.selectedDate = persianDate;
                    pickerState.gregorianDate = new Date(year, month - 1, day).toISOString().slice(0, 19);

                    this.updateDisplayInput(pickerState);
                    this.updateCalendar(pickerState);

                    // Update hidden field
                    if (pickerState.hiddenField) {
                        pickerState.hiddenField.value = pickerState.gregorianDate;
                    }

                    this.hideDatepicker(pickerState);
                }
            } catch (error) {
                console.error('Error converting Gregorian date:', error);
            }
        }

        async convertGregorianToPersian(year, month, day) {
            try {
                const response = await fetch('/Base/ConvertGregorianDate', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
                    },
                    body: JSON.stringify({ year: year, month: month, day: day })
                });

                const result = await response.json();

                if (result.success) {
                    console.log('Gregorian to Persian conversion:', result);
                    return result.persianDate;
                } else {
                    console.error('Server conversion error:', result.error);
                    return null;
                }
            } catch (error) {
                console.error('Error calling Gregorian conversion API:', error);
                return null;
            }
        }

        async updateHiddenField(pickerState) {
            if (pickerState.selectedDate) {
                const { year, month, day } = pickerState.selectedDate;
                const persianDateString = `${year}/${month.toString().padStart(2, '0')}/${day.toString().padStart(2, '0')}`;

                // Convert using server API
                const result = await this.convertPersianToGregorian(persianDateString);

                if (result) {
                    console.log(`Persian: ${persianDateString} -> Gregorian DateTime: ${result}`);

                    // Store the full DateTime with server time preserved
                    pickerState.gregorianDate = result;

                    // Verify consistency - parse the result and log for debugging
                    const parsedDate = new Date(result);
                    if (!isNaN(parsedDate.getTime())) {
                        const verifyDate = `${parsedDate.getFullYear()}/${(parsedDate.getMonth() + 1).toString().padStart(2, '0')}/${parsedDate.getDate().toString().padStart(2, '0')}`;
                        console.log(`Verification - Parsed Gregorian: ${verifyDate}`);
                    }

                    if (pickerState.hiddenField) {
                        pickerState.hiddenField.value = result;
                    } else {
                        const hiddenField = document.createElement('input');
                        hiddenField.type = 'hidden';
                        hiddenField.name = pickerState.originalName || 'RequiredDate';
                        hiddenField.className = 'persian-date-hidden';
                        hiddenField.value = result;

                        pickerState.wrapper.appendChild(hiddenField);
                        pickerState.hiddenField = hiddenField;
                    }
                } else {
                    console.error('Failed to convert Persian date to Gregorian');
                }
            }
        }

        async updateHiddenField(pickerState) {
            if (pickerState.selectedDate) {
                const { year, month, day } = pickerState.selectedDate;
                const persianDateString = `${year}/${month.toString().padStart(2, '0')}/${day.toString().padStart(2, '0')}`;

                // Convert using server API
                const gregorianDate = await this.convertPersianToGregorian(persianDateString);

                if (gregorianDate) {
                    console.log(`Persian: ${persianDateString} -> Gregorian: ${gregorianDate}`);

                    // Store the Gregorian date for toggle functionality
                    pickerState.gregorianDate = gregorianDate;

                    if (pickerState.hiddenField) {
                        pickerState.hiddenField.value = gregorianDate;
                    } else {
                        const hiddenField = document.createElement('input');
                        hiddenField.type = 'hidden';
                        hiddenField.name = pickerState.originalName || 'RequiredDate';
                        hiddenField.className = 'persian-date-hidden';
                        hiddenField.value = gregorianDate;

                        pickerState.wrapper.appendChild(hiddenField);
                        pickerState.hiddenField = hiddenField;
                    }
                } else {
                    console.error('Failed to convert Persian date to Gregorian');
                }
            }
        }

        previousMonth(pickerState) {
            if (pickerState.calendarType === 'gregorian') {
                pickerState.viewMonth--;
                if (pickerState.viewMonth < 1) {
                    pickerState.viewMonth = 12;
                    pickerState.viewYear--;
                }
            } else {
                pickerState.viewMonth--;
                if (pickerState.viewMonth < 1) {
                    pickerState.viewMonth = 12;
                    pickerState.viewYear--;
                }
            }
            this.updateCalendar(pickerState);
        }

        nextMonth(pickerState) {
            if (pickerState.calendarType === 'gregorian') {
                pickerState.viewMonth++;
                if (pickerState.viewMonth > 12) {
                    pickerState.viewMonth = 1;
                    pickerState.viewYear++;
                }
            } else {
                pickerState.viewMonth++;
                if (pickerState.viewMonth > 12) {
                    pickerState.viewMonth = 1;
                    pickerState.viewYear++;
                }
            }
            this.updateCalendar(pickerState);
        }

        // Direct hidden field update to avoid recursive API calls
        updateHiddenFieldDirectly(pickerState, gregorianDateTime) {
            if (pickerState.hiddenField) {
                pickerState.hiddenField.value = gregorianDateTime;
            } else {
                const hiddenField = document.createElement('input');
                hiddenField.type = 'hidden';
                hiddenField.name = pickerState.originalName || 'RequiredDate';
                hiddenField.className = 'persian-date-hidden';
                hiddenField.value = gregorianDateTime;

                pickerState.wrapper.appendChild(hiddenField);
                pickerState.hiddenField = hiddenField;
            }

            console.log(`Hidden field updated directly: ${gregorianDateTime}`);
        }

        goToToday(pickerState) {
            if (this.currentDate) {
                pickerState.viewYear = this.currentDate.year;
                pickerState.viewMonth = this.currentDate.month;
                this.selectDate(pickerState, this.currentDate.year, this.currentDate.month, this.currentDate.day);
            }
        }
    }

    // Initialize when DOM is ready
    function initializePersianDatePickers() {
        const dateInputs = document.querySelectorAll('input[data-persian-date="true"]');
        dateInputs.forEach(input => {
            if (window.PersianDatePicker && !window.PersianDatePicker.activePickers.has(input)) {
                window.PersianDatePicker.initializeDatePicker(input);
            }
        });
    }

    // Initialize when everything is ready
    document.addEventListener('DOMContentLoaded', function () {
        window.PersianDatePicker = new PersianDatePicker();

        // Give it a moment to initialize
        setTimeout(() => {
            window.initializePersianDatePickers = initializePersianDatePickers;
            initializePersianDatePickers();
        }, 100);

        // Close datepickers when clicking outside
        document.addEventListener('click', function (e) {
            if (!e.target.closest('.persian-date-wrapper') && window.PersianDatePicker) {
                window.PersianDatePicker.activePickers.forEach((pickerState) => {
                    window.PersianDatePicker.hideDatepicker(pickerState);
                });
            }
        });
    });

})(window, document);