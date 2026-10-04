/* =====================================================
   site.js — GymZone Pro Frontend Logic
   ===================================================== */

// ── Toast Notifications ─────────────────────────────
const Toast = {
  show(message, type = 'success', duration = 4000) {
    if (typeof Swal !== 'undefined') {
      Swal.fire({
        toast: true,
        position: 'top-end',
        icon: type === 'info' ? 'info' : type,
        title: message,
        showConfirmButton: false,
        timer: duration,
        timerProgressBar: true,
        background: 'rgba(15, 15, 20, 0.95)',
        color: '#fff',
        customClass: {
          popup: 'glass-card border border-gym-blue/30 shadow-[0_0_20px_rgba(0,229,255,0.15)]'
        },
        didOpen: (toast) => {
          toast.addEventListener('mouseenter', Swal.stopTimer)
          toast.addEventListener('mouseleave', Swal.resumeTimer)
        }
      });
    } else {
      alert(message);
    }
  }
};

// ── Check for server-side flash messages ────────────
document.addEventListener('DOMContentLoaded', () => {
  const successMsg = document.getElementById('server-success');
  const errorMsg = document.getElementById('server-error');
  if (successMsg?.dataset.msg) Toast.show(successMsg.dataset.msg, 'success');
  if (errorMsg?.dataset.msg) Toast.show(errorMsg.dataset.msg, 'error');
});

// ── AJAX Check-In ───────────────────────────────────
const CheckIn = {
  currentMember: null,

  async searchMember(term) {
    if (!term || term.length < 2) {
      document.getElementById('member-result')?.classList.add('d-none');
      return;
    }

    try {
      const res = await fetch(`/Attendance/MemberStatus?memberId=${encodeURIComponent(term)}`);
      const data = await res.json();

      const resultEl = document.getElementById('member-result');
      if (!resultEl) return;

      if (!data.found) {
        resultEl.innerHTML = `<div class="alert-glass alert-danger-glass">Member not found.</div>`;
        resultEl.classList.remove('d-none');
        return;
      }

      this.currentMember = data;
      const statusClass = data.status === 'Active' ? 'badge-active' : 'badge-inactive';
      const membershipInfo = data.hasMembership
        ? `<span style="color:#00FF88">✓ ${data.planName} — Expires ${data.expiryDate}</span>`
        : `<span style="color:#FF006E">✗ No active membership</span>`;

      resultEl.innerHTML = `
        <div class="glass-card" style="padding:16px">
          <div style="display:flex;align-items:center;gap:12px;margin-bottom:12px">
            <div class="avatar">${data.fullName.charAt(0)}</div>
            <div>
              <div style="font-weight:600;color:#F0F0F8">${data.fullName}</div>
              <div style="font-size:12px;color:#9994AA">${data.membershipNumber} • ${data.phone}</div>
            </div>
            <span class="badge-status ${statusClass}" style="margin-left:auto">${data.status}</span>
          </div>
          <div style="font-size:13px;margin-bottom:12px">${membershipInfo}</div>
          ${data.checkedIn
          ? `<button class="btn-gradient-blue" onclick="CheckIn.checkout(${data.attendanceId})" style="width:100%">
               <i class="bi bi-box-arrow-right"></i> Check Out
             </button>`
          : `<button class="btn-gradient-blue" onclick="CheckIn.doCheckIn(${data.memberId})" style="width:100%" ${!data.hasMembership ? 'style="opacity:0.5"' : ''}>
               <i class="bi bi-box-arrow-in-right"></i> Check In
             </button>`
        }
        </div>
      `;
      resultEl.classList.remove('d-none');
    } catch (e) {
      console.error('Search failed:', e);
    }
  },

  async doCheckIn(memberId) {
    const batchId = document.getElementById('batch-select')?.value || null;
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    try {
      const res = await fetch('/Attendance/DoCheckIn', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `memberId=${memberId}&batchId=${batchId || ''}&__RequestVerificationToken=${encodeURIComponent(token || '')}`
      });
      const data = await res.json();
      Toast.show(data.message, data.success ? 'success' : 'error');
      if (data.success) {
        document.getElementById('search-input')?.dispatchEvent(new Event('input'));
      }
    } catch (e) {
      Toast.show('Check-in failed. Please try again.', 'error');
    }
  },

  async checkout(attendanceId) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    try {
      const res = await fetch('/Attendance/DoCheckOut', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `attendanceId=${attendanceId}&__RequestVerificationToken=${encodeURIComponent(token || '')}`
      });
      const data = await res.json();
      Toast.show(data.message, data.success ? 'success' : 'error');
      if (data.success) {
        document.getElementById('search-input')?.value && CheckIn.searchMember(document.getElementById('search-input').value);
      }
    } catch (e) {
      Toast.show('Check-out failed.', 'error');
    }
  }
};

// ── Member Search Autocomplete ──────────────────────
async function memberSearch(term, listId) {
  if (!term || term.length < 2) return;
  try {
    const res = await fetch(`/Member/Search?term=${encodeURIComponent(term)}`);
    const data = await res.json();
    const list = document.getElementById(listId);
    if (!list) return;
    list.innerHTML = data.map(m => `
      <div class="search-suggestion" onclick="selectMember(${m.memberId}, '${m.fullName}', '${m.membershipNumber}')"
        style="padding:10px 14px;cursor:pointer;border-bottom:1px solid rgba(255,255,255,0.05);transition:all 0.15s"
        onmouseenter="this.style.background='rgba(0,212,255,0.08)'"
        onmouseleave="this.style.background=''">
        <div style="font-weight:600;color:#F0F0F8">${m.fullName}</div>
        <div style="font-size:11px;color:#9994AA">${m.membershipNumber} • ${m.phone}</div>
      </div>
    `).join('');
    list.classList.remove('d-none');
  } catch (e) {
    console.error('Member search failed:', e);
  }
}

function selectMember(id, name, number) {
  const hiddenInput = document.getElementById('member-id-input');
  const displayInput = document.getElementById('member-display-input');
  if (hiddenInput) hiddenInput.value = id;
  if (displayInput) displayInput.value = `${name} (${number})`;
  const list = document.getElementById('member-suggestions');
  if (list) list.classList.add('d-none');
}

// ── Notification Count ──────────────────────────────
async function loadNotificationCount() {
  try {
    const res = await fetch('/Notification/Count');
    const data = await res.json();
    const badge = document.getElementById('notification-count');
    if (badge) {
      badge.textContent = data.count;
      badge.style.display = data.count > 0 ? 'flex' : 'none';
    }
  } catch (e) { }
}

// Poll every 60 seconds
loadNotificationCount();
setInterval(loadNotificationCount, 60000);

// ── Chart.js Global Config ──────────────────────────
if (typeof Chart !== 'undefined') {
  Chart.defaults.color = '#9994AA';
  Chart.defaults.borderColor = 'rgba(255,255,255,0.07)';
  Chart.defaults.font.family = "'Inter', sans-serif";

  const gradientPlugin = {
    id: 'customCanvasBackgroundColor',
    beforeDraw(chart) {
      const { ctx, chartArea } = chart;
      if (!chartArea) return;
    }
  };

  // Register custom tooltip styling
  Chart.defaults.plugins.tooltip.backgroundColor = 'rgba(17,17,28,0.95)';
  Chart.defaults.plugins.tooltip.borderColor = 'rgba(255,255,255,0.1)';
  Chart.defaults.plugins.tooltip.borderWidth = 1;
  Chart.defaults.plugins.tooltip.titleColor = '#F0F0F8';
  Chart.defaults.plugins.tooltip.bodyColor = '#9994AA';
  Chart.defaults.plugins.tooltip.padding = 12;
  Chart.defaults.plugins.tooltip.cornerRadius = 8;

  Chart.defaults.plugins.legend.labels.usePointStyle = true;
  Chart.defaults.plugins.legend.labels.pointStyleWidth = 8;
}

// ── Helper: Create line chart ──────────────────────
function createLineChart(canvasId, labels, datasets, options = {}) {
  const ctx = document.getElementById(canvasId)?.getContext('2d');
  if (!ctx) return;
  return new Chart(ctx, {
    type: 'line',
    data: { labels, datasets },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      scales: {
        x: { grid: { display: false } },
        y: { grid: { color: 'rgba(255,255,255,0.05)' }, beginAtZero: true }
      },
      plugins: { legend: { display: datasets.length > 1 } },
      ...options
    }
  });
}

function createBarChart(canvasId, labels, datasets, options = {}) {
  const ctx = document.getElementById(canvasId)?.getContext('2d');
  if (!ctx) return;
  return new Chart(ctx, {
    type: 'bar',
    data: { labels, datasets },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      scales: {
        x: { grid: { display: false } },
        y: { grid: { color: 'rgba(255,255,255,0.05)' }, beginAtZero: true }
      },
      plugins: { legend: { display: datasets.length > 1 } },
      ...options
    }
  });
}

function createDoughnutChart(canvasId, labels, data, colors) {
  const ctx = document.getElementById(canvasId)?.getContext('2d');
  if (!ctx) return;
  return new Chart(ctx, {
    type: 'doughnut',
    data: {
      labels,
      datasets: [{ data, backgroundColor: colors, borderColor: 'rgba(10,10,15,0.8)', borderWidth: 2 }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: { legend: { position: 'bottom' } },
      cutout: '70%'
    }
  });
}

// ── Form Helpers ────────────────────────────────────
function confirmDelete(message = 'Are you sure you want to delete this item?') {
  return confirm(message);
}

// ── Sidebar toggle (mobile) ─────────────────────────
function toggleSidebar() {
  document.querySelector('.sidebar')?.classList.toggle('open');
}

// ── Auto-dismiss alerts ──────────────────────────────
document.querySelectorAll('.auto-dismiss').forEach(el => {
  setTimeout(() => el.remove(), 5000);
});

// Anime.js v4 logic is now handled via ES modules in _Layout.cshtml

// ── Global Loader ────────────────────────────────────
document.addEventListener('submit', function(e) {
  if (e.defaultPrevented) return;
  if (e.target.classList.contains('no-loader') || e.target.target === '_blank') return;
  
  if (typeof jQuery !== 'undefined' && jQuery(e.target).length && typeof jQuery(e.target).valid === 'function') {
    if (!jQuery(e.target).valid()) return;
  }

  const loader = document.getElementById('global-loader');
  if (loader) {
    loader.classList.remove('hidden');
    setTimeout(() => {
      loader.classList.remove('opacity-0');
    }, 10);
  }
});

window.addEventListener('pageshow', function(event) {
  const loader = document.getElementById('global-loader');
  if (loader && !loader.classList.contains('hidden')) {
    loader.classList.add('opacity-0');
    setTimeout(() => loader.classList.add('hidden'), 300);
  }
});
