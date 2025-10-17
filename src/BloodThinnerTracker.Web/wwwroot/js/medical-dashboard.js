// Blood Thinner Tracker - Chart and UI JavaScript Functions

// Chart.js Configuration for INR Tracking
window.renderINRChart = function(containerId, data) {
    const container = document.getElementById(containerId);
    if (!container || !data || data.length === 0) return;

    // Clear existing chart
    container.innerHTML = '';

    // Create canvas element
    const canvas = document.createElement('canvas');
    container.appendChild(canvas);

    const ctx = canvas.getContext('2d');

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.map(d => {
                const date = new Date(d.date);
                return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
            }),
            datasets: [{
                label: 'INR Value',
                data: data.map(d => d.value),
                borderColor: '#007bff',
                backgroundColor: 'rgba(0, 123, 255, 0.1)',
                borderWidth: 3,
                fill: false,
                tension: 0.4,
                pointBackgroundColor: '#007bff',
                pointBorderColor: '#fff',
                pointBorderWidth: 2,
                pointRadius: 6,
                pointHoverRadius: 8
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    titleColor: '#fff',
                    bodyColor: '#fff',
                    borderColor: '#007bff',
                    borderWidth: 1,
                    displayColors: false,
                    callbacks: {
                        title: function(context) {
                            const dataIndex = context[0].dataIndex;
                            const date = new Date(data[dataIndex].date);
                            return date.toLocaleDateString('en-US', { 
                                weekday: 'long', 
                                year: 'numeric', 
                                month: 'long', 
                                day: 'numeric' 
                            });
                        },
                        label: function(context) {
                            const value = context.parsed.y;
                            let status = 'In Range';
                            if (value < 2.0) status = 'Below Range (Risk of Clots)';
                            else if (value > 3.0) status = 'Above Range (Risk of Bleeding)';
                            
                            return [
                                `INR: ${value.toFixed(1)}`,
                                `Status: ${status}`,
                                `Target: 2.0 - 3.0`
                            ];
                        }
                    }
                }
            },
            scales: {
                x: {
                    grid: {
                        display: false
                    },
                    ticks: {
                        color: '#6c757d'
                    }
                },
                y: {
                    beginAtZero: false,
                    min: 1.0,
                    max: 4.0,
                    grid: {
                        color: 'rgba(0, 0, 0, 0.1)'
                    },
                    ticks: {
                        color: '#6c757d',
                        callback: function(value) {
                            return value.toFixed(1);
                        }
                    }
                }
            },
            elements: {
                point: {
                    hoverBackgroundColor: function(context) {
                        const value = context.parsed.y;
                        if (value < 2.0) return '#ffc107';
                        else if (value > 3.0) return '#dc3545';
                        return '#28a745';
                    }
                }
            }
        }
    });
};

// Advanced INR Trend Chart with Target Range
window.renderINRTrendChart = function(containerId, data) {
    const container = document.getElementById(containerId);
    if (!container || !data || data.length === 0) return;

    // Clear existing chart
    container.innerHTML = '';

    // Create canvas element
    const canvas = document.createElement('canvas');
    container.appendChild(canvas);

    const ctx = canvas.getContext('2d');

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.map(d => {
                const date = new Date(d.date);
                return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
            }),
            datasets: [
                {
                    label: 'Target Range Max',
                    data: data.map(d => d.targetMax),
                    borderColor: 'rgba(40, 167, 69, 0.3)',
                    backgroundColor: 'rgba(40, 167, 69, 0.1)',
                    borderWidth: 1,
                    fill: '+1',
                    pointRadius: 0,
                    pointHoverRadius: 0,
                    borderDash: [5, 5]
                },
                {
                    label: 'Target Range Min',
                    data: data.map(d => d.targetMin),
                    borderColor: 'rgba(40, 167, 69, 0.3)',
                    backgroundColor: 'rgba(40, 167, 69, 0.1)',
                    borderWidth: 1,
                    fill: false,
                    pointRadius: 0,
                    pointHoverRadius: 0,
                    borderDash: [5, 5]
                },
                {
                    label: 'INR Value',
                    data: data.map(d => d.value),
                    borderColor: '#007bff',
                    backgroundColor: 'rgba(0, 123, 255, 0.1)',
                    borderWidth: 3,
                    fill: false,
                    tension: 0.4,
                    pointBackgroundColor: data.map(d => {
                        if (d.value < d.targetMin) return '#ffc107';
                        else if (d.value > d.targetMax) return '#dc3545';
                        return '#28a745';
                    }),
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    pointRadius: 6,
                    pointHoverRadius: 8
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    backgroundColor: 'rgba(0, 0, 0, 0.8)',
                    titleColor: '#fff',
                    bodyColor: '#fff',
                    borderColor: '#007bff',
                    borderWidth: 1,
                    displayColors: false,
                    filter: function(tooltipItem) {
                        return tooltipItem.datasetIndex === 2; // Only show tooltip for INR values
                    },
                    callbacks: {
                        title: function(context) {
                            const dataIndex = context[0].dataIndex;
                            const date = new Date(data[dataIndex].date);
                            return date.toLocaleDateString('en-US', { 
                                weekday: 'long', 
                                year: 'numeric', 
                                month: 'long', 
                                day: 'numeric' 
                            });
                        },
                        label: function(context) {
                            const value = context.parsed.y;
                            const targetMin = data[context.dataIndex].targetMin;
                            const targetMax = data[context.dataIndex].targetMax;
                            
                            let status = 'In Target Range';
                            let risk = '';
                            if (value < targetMin) {
                                status = 'Below Target Range';
                                risk = 'Risk: Blood clots';
                            } else if (value > targetMax) {
                                status = 'Above Target Range';
                                risk = 'Risk: Bleeding';
                            }
                            
                            const result = [
                                `INR: ${value.toFixed(1)}`,
                                `Target: ${targetMin.toFixed(1)} - ${targetMax.toFixed(1)}`,
                                `Status: ${status}`
                            ];
                            
                            if (risk) result.push(risk);
                            return result;
                        }
                    }
                }
            },
            scales: {
                x: {
                    grid: {
                        display: false
                    },
                    ticks: {
                        color: '#6c757d',
                        maxTicksLimit: 8
                    }
                },
                y: {
                    beginAtZero: false,
                    min: 1.0,
                    max: 4.5,
                    grid: {
                        color: 'rgba(0, 0, 0, 0.1)'
                    },
                    ticks: {
                        color: '#6c757d',
                        callback: function(value) {
                            return value.toFixed(1);
                        }
                    }
                }
            },
            interaction: {
                intersect: false,
                mode: 'index'
            }
        }
    });
};

// Medication Adherence Chart
window.renderAdherenceChart = function(containerId, data) {
    const container = document.getElementById(containerId);
    if (!container || !data) return;

    container.innerHTML = '';
    const canvas = document.createElement('canvas');
    container.appendChild(canvas);

    const ctx = canvas.getContext('2d');

    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Taken', 'Missed', 'Delayed'],
            datasets: [{
                data: [data.taken, data.missed, data.delayed],
                backgroundColor: ['#28a745', '#dc3545', '#ffc107'],
                borderWidth: 2,
                borderColor: '#fff'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        padding: 20,
                        usePointStyle: true
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = ((context.parsed / total) * 100).toFixed(1);
                            return `${context.label}: ${context.parsed} (${percentage}%)`;
                        }
                    }
                }
            },
            cutout: '60%'
        }
    });
};

// Utility Functions
window.medicalDashboard = {
    // Format date for display
    formatDate: function(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString('en-US', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
    },

    // Format time for display
    formatTime: function(timeString) {
        const time = new Date(`2000-01-01T${timeString}`);
        return time.toLocaleTimeString('en-US', {
            hour: 'numeric',
            minute: '2-digit',
            hour12: true
        });
    },

    // Calculate time in range percentage
    calculateTimeInRange: function(values, targetMin, targetMax) {
        if (!values || values.length === 0) return 0;
        
        const inRange = values.filter(v => v >= targetMin && v <= targetMax).length;
        return Math.round((inRange / values.length) * 100);
    },

    // Show notification
    showNotification: function(message, type = 'info') {
        const toast = document.createElement('div');
        toast.className = `toast align-items-center text-white bg-${type} border-0`;
        toast.setAttribute('role', 'alert');
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        `;

        const container = document.getElementById('toast-container') || this.createToastContainer();
        container.appendChild(toast);

        const bsToast = new bootstrap.Toast(toast);
        bsToast.show();

        toast.addEventListener('hidden.bs.toast', () => {
            toast.remove();
        });
    },

    // Create toast container if it doesn't exist
    createToastContainer: function() {
        const container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container position-fixed top-0 end-0 p-3';
        container.style.zIndex = '1100';
        document.body.appendChild(container);
        return container;
    },

    // Confirm dialog with custom styling
    confirmDialog: function(message, title = 'Confirm Action') {
        return new Promise((resolve) => {
            const modal = document.createElement('div');
            modal.className = 'modal fade';
            modal.innerHTML = `
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">${title}</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <p>${message}</p>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                            <button type="button" class="btn btn-primary confirm-btn">Confirm</button>
                        </div>
                    </div>
                </div>
            `;

            document.body.appendChild(modal);

            const bsModal = new bootstrap.Modal(modal);
            bsModal.show();

            modal.querySelector('.confirm-btn').addEventListener('click', () => {
                resolve(true);
                bsModal.hide();
            });

            modal.addEventListener('hidden.bs.modal', () => {
                if (!modal.dataset.confirmed) resolve(false);
                modal.remove();
            });

            modal.querySelector('.confirm-btn').addEventListener('click', () => {
                modal.dataset.confirmed = 'true';
            });
        });
    },

    // Auto-refresh data
    startAutoRefresh: function(callback, interval = 300000) { // 5 minutes default
        return setInterval(callback, interval);
    },

    // Stop auto-refresh
    stopAutoRefresh: function(intervalId) {
        if (intervalId) {
            clearInterval(intervalId);
        }
    },

    // Local storage helpers
    storage: {
        set: function(key, value) {
            try {
                localStorage.setItem(`bloodthinner_${key}`, JSON.stringify(value));
            } catch (e) {
                console.warn('LocalStorage not available:', e);
            }
        },

        get: function(key) {
            try {
                const item = localStorage.getItem(`bloodthinner_${key}`);
                return item ? JSON.parse(item) : null;
            } catch (e) {
                console.warn('LocalStorage not available:', e);
                return null;
            }
        },

        remove: function(key) {
            try {
                localStorage.removeItem(`bloodthinner_${key}`);
            } catch (e) {
                console.warn('LocalStorage not available:', e);
            }
        }
    }
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    // Initialize tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Initialize popovers
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });

    // Add smooth scrolling to anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    // Auto-hide alerts after 5 seconds
    document.querySelectorAll('.alert:not(.alert-permanent)').forEach(alert => {
        setTimeout(() => {
            alert.style.transition = 'opacity 0.5s';
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 500);
        }, 5000);
    });
});

// Export for global access
window.MedicalDashboard = window.medicalDashboard;