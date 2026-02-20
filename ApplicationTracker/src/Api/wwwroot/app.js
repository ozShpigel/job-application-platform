// ============================================================
// APPLICATION TRACKER - Frontend
// ============================================================

const API = '/api';

// Hebrew translations for statuses
const STATUS_HE = {
    Analyzing: 'בניתוח',
    DecidedToApply: 'החלטתי להגיש',
    Applied: 'הוגש',
    PhoneScreen: 'שיחת טלפון',
    TechnicalInterview: 'ראיון טכני',
    FinalRound: 'סבב אחרון',
    OfferReceived: 'הצעה התקבלה',
    Accepted: 'התקבלתי',
    Rejected: 'נדחה',
    Withdrawn: 'נסוג'
};

const STATUS_COLORS = {
    Analyzing: 'analyzing',
    DecidedToApply: 'decidedtoapply',
    Applied: 'applied',
    PhoneScreen: 'phonescreen',
    TechnicalInterview: 'technicalinterview',
    FinalRound: 'finalround',
    OfferReceived: 'offerreceived',
    Accepted: 'accepted',
    Rejected: 'rejected',
    Withdrawn: 'withdrawn'
};

const INTERVIEW_TYPES = ['Phone', 'Technical', 'Final', 'HR'];
const NOTE_CATEGORIES = ['Preparation', 'Research', 'Thoughts', 'Follow-up'];
const NOTE_CATEGORIES_HE = {
    Preparation: 'הכנה',
    Research: 'מחקר',
    Thoughts: 'מחשבות',
    'Follow-up': 'מעקב'
};

// ============================================================
// INIT
// ============================================================

document.addEventListener('DOMContentLoaded', () => {
    initTabs();
    populateStatusSelect('add-status');
    loadDashboard();

    document.getElementById('add-form').addEventListener('submit', handleAddApplication);
});

// ============================================================
// TABS
// ============================================================

function initTabs() {
    document.querySelectorAll('.tab').forEach(tab => {
        tab.addEventListener('click', () => showTab(tab.dataset.tab));
    });
}

function showTab(name) {
    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
    document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));

    const tabBtn = document.querySelector(`.tab[data-tab="${name}"]`);
    if (tabBtn) tabBtn.classList.add('active');
    document.getElementById(`tab-${name}`).classList.add('active');

    // Load data when tab becomes active
    if (name === 'dashboard') loadDashboard();
    if (name === 'list') loadApplicationList();
    if (name === 'stats') loadStats();
}

// ============================================================
// API HELPERS
// ============================================================

async function api(path, options = {}) {
    const res = await fetch(`${API}${path}`, {
        headers: { 'Content-Type': 'application/json' },
        ...options
    });
    if (!res.ok && res.status !== 204) {
        const err = await res.text();
        throw new Error(err || `HTTP ${res.status}`);
    }
    if (res.status === 204) return null;
    return res.json();
}

function formatDate(dateStr) {
    if (!dateStr) return '-';
    const d = new Date(dateStr);
    return `${d.getDate().toString().padStart(2, '0')}/${(d.getMonth() + 1).toString().padStart(2, '0')}/${d.getFullYear()}`;
}

function formatDateTime(dateStr) {
    if (!dateStr) return '-';
    const d = new Date(dateStr);
    return `${formatDate(dateStr)} ${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
}

function statusBadge(status) {
    const cssClass = STATUS_COLORS[status] || 'analyzing';
    const label = STATUS_HE[status] || status;
    return `<span class="badge badge-${cssClass}">${label}</span>`;
}

function populateStatusSelect(id) {
    const select = document.getElementById(id);
    select.innerHTML = Object.entries(STATUS_HE)
        .map(([val, label]) => `<option value="${val}">${label}</option>`)
        .join('');
}

function scoreColor(score) {
    if (score == null) return 'var(--text-dim)';
    if (score >= 60) return 'var(--green)';
    if (score >= 40) return 'var(--yellow)';
    return 'var(--red)';
}

// ============================================================
// DASHBOARD
// ============================================================

async function loadDashboard() {
    try {
        const [stats, upcoming] = await Promise.all([
            api('/stats'),
            api('/interviews/upcoming')
        ]);

        document.getElementById('summary-cards').innerHTML = `
            <div class="summary-card">
                <div class="value">${stats.total}</div>
                <div class="label">סה"כ משרות</div>
            </div>
            <div class="summary-card">
                <div class="value">${stats.inProgress}</div>
                <div class="label">בתהליך</div>
            </div>
            <div class="summary-card">
                <div class="value">${stats.avgScore || '-'}</div>
                <div class="label">ציון ממוצע</div>
            </div>
            <div class="summary-card">
                <div class="value">${stats.responseRate}%</div>
                <div class="label">אחוז מענה</div>
            </div>
        `;

        // Upcoming interviews
        if (upcoming.length === 0) {
            document.getElementById('upcoming-interviews').innerHTML = '<p class="empty-state">אין ראיונות קרובים</p>';
        } else {
            document.getElementById('upcoming-interviews').innerHTML = upcoming.map(u => `
                <div class="item-card">
                    <div class="item-header">
                        <span class="item-title">${u.interview.type} - ${u.company || ''}</span>
                        <span class="item-meta">${formatDateTime(u.interview.scheduledAt)}</span>
                    </div>
                    <div class="item-body text-dim">${u.jobTitle || ''} ${u.interview.interviewer ? '| ' + u.interview.interviewer : ''}</div>
                </div>
            `).join('');
        }

        // Recent activity - load all apps and show latest status updates
        const apps = await api('/applications');
        if (apps.length === 0) {
            document.getElementById('recent-activity').innerHTML = '<p class="empty-state">אין פעילות אחרונה</p>';
        } else {
            const recent = apps.slice(0, 5);
            document.getElementById('recent-activity').innerHTML = recent.map(a => `
                <div class="timeline-item" style="cursor:pointer" onclick="showDetail('${a.id}')">
                    <div class="timeline-icon status">📋</div>
                    <div class="timeline-content">
                        <div class="timeline-text"><strong>${a.jobTitle}</strong> - ${a.company} ${statusBadge(a.status)}</div>
                        <div class="timeline-date">${formatDate(a.createdAt)}</div>
                    </div>
                </div>
            `).join('');
        }
    } catch (e) {
        console.error('Dashboard error:', e);
    }
}

// ============================================================
// APPLICATION LIST
// ============================================================

async function loadApplicationList() {
    try {
        const apps = await api('/applications');
        const container = document.getElementById('app-list');

        if (apps.length === 0) {
            container.innerHTML = '<p class="empty-state">אין משרות עדיין. הוסף משרה חדשה!</p>';
            return;
        }

        container.innerHTML = apps.map(a => `
            <div class="app-row" onclick="showDetail('${a.id}')">
                <div>
                    <div class="title">${a.jobTitle}</div>
                </div>
                <div class="company">${a.company}</div>
                <div>${statusBadge(a.status)}</div>
                <div class="score" style="color:${scoreColor(a.matchScore)}">${a.matchScore ?? '-'}</div>
                <div class="date">${formatDate(a.createdAt)}</div>
            </div>
        `).join('');
    } catch (e) {
        console.error('List error:', e);
    }
}

// ============================================================
// ADD APPLICATION
// ============================================================

async function handleAddApplication(e) {
    e.preventDefault();
    const btn = e.target.querySelector('button[type="submit"]');
    btn.disabled = true;
    btn.textContent = 'שומר...';

    try {
        const score = document.getElementById('add-score').value;
        const app = {
            jobTitle: document.getElementById('add-title').value,
            company: document.getElementById('add-company').value,
            status: document.getElementById('add-status').value,
            matchScore: score ? parseInt(score) : null,
            matchVerdict: document.getElementById('add-verdict').value || null,
            jobDescription: document.getElementById('add-description').value || null
        };

        await api('/applications', {
            method: 'POST',
            body: JSON.stringify(app)
        });

        e.target.reset();
        showTab('list');
    } catch (err) {
        alert('שגיאה בשמירה: ' + err.message);
    } finally {
        btn.disabled = false;
        btn.textContent = 'הוסף משרה';
    }
}

// ============================================================
// APPLICATION DETAIL
// ============================================================

async function showDetail(id) {
    try {
        const data = await api(`/applications/${id}`);
        const app = data.application;
        const interviews = data.interviews;
        const notes = data.notes;
        const statusUpdates = data.statusUpdates;

        const container = document.getElementById('detail-content');
        container.innerHTML = `
            <div class="card">
                <div class="detail-header">
                    <div>
                        <h2>${app.jobTitle}</h2>
                        <div class="company-name">${app.company}</div>
                        <div class="mt-1">${statusBadge(app.status)}</div>
                    </div>
                    <div style="text-align:center">
                        <div class="detail-score" style="color:${scoreColor(app.matchScore)}">${app.matchScore ?? '-'}</div>
                        <div class="text-dim text-sm">${app.matchVerdict || ''}</div>
                    </div>
                </div>

                <div class="btn-group mt-1">
                    <button class="btn btn-primary btn-sm" onclick="showStatusModal('${app.id}', '${app.status}')">עדכן סטטוס</button>
                    <button class="btn btn-secondary btn-sm" onclick="showInterviewModal('${app.id}')">הוסף ראיון</button>
                    <button class="btn btn-secondary btn-sm" onclick="showNoteModal('${app.id}')">הוסף הערה</button>
                    <button class="btn btn-danger btn-sm" onclick="deleteApplication('${app.id}')">מחק</button>
                </div>
            </div>

            <!-- AI ANALYSIS -->
            ${renderAnalysis(app.matchAnalysis)}

            <!-- TIMELINE -->
            <div class="card">
                <h3 class="section-title">ציר זמן</h3>
                <div id="detail-timeline">${renderTimeline(statusUpdates, interviews, notes)}</div>
            </div>

            <!-- INTERVIEWS -->
            <div class="card">
                <div class="collapsible-header" onclick="toggleCollapse(this)">
                    <h3 class="section-title" style="border:none;margin:0;padding:0">ראיונות (${interviews.length})</h3>
                </div>
                <div class="collapsible-body">
                    ${interviews.length === 0 ? '<p class="text-dim text-sm">אין ראיונות</p>' :
                        interviews.map(i => renderInterviewCard(i)).join('')}
                </div>
            </div>

            <!-- NOTES -->
            <div class="card">
                <div class="collapsible-header" onclick="toggleCollapse(this)">
                    <h3 class="section-title" style="border:none;margin:0;padding:0">הערות (${notes.length})</h3>
                </div>
                <div class="collapsible-body">
                    ${notes.length === 0 ? '<p class="text-dim text-sm">אין הערות</p>' :
                        notes.map(n => renderNoteCard(n)).join('')}
                </div>
            </div>

            <!-- JOB DESCRIPTION -->
            ${app.jobDescription ? `
            <div class="card">
                <div class="collapsible-header collapsed" onclick="toggleCollapse(this)">
                    <h3 class="section-title" style="border:none;margin:0;padding:0">תיאור המשרה</h3>
                </div>
                <div class="collapsible-body hidden">
                    <pre style="white-space:pre-wrap;font-family:inherit;font-size:0.85rem;color:var(--text-dim)">${app.jobDescription}</pre>
                </div>
            </div>` : ''}
        `;

        // Show detail tab
        document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
        document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
        document.getElementById('tab-detail').classList.add('active');
    } catch (e) {
        console.error('Detail error:', e);
    }
}

function renderTimeline(statusUpdates, interviews, notes) {
    const items = [];

    statusUpdates.forEach(s => items.push({
        date: s.timestamp,
        html: `<div class="timeline-item">
            <div class="timeline-icon status">📊</div>
            <div class="timeline-content">
                <div class="timeline-text">${statusBadge(s.fromStatus)} ← ${statusBadge(s.toStatus)}</div>
                ${s.note ? `<div class="timeline-text text-dim">${s.note}</div>` : ''}
                <div class="timeline-date">${formatDateTime(s.timestamp)}</div>
            </div>
        </div>`
    }));

    interviews.forEach(i => items.push({
        date: i.scheduledAt,
        html: `<div class="timeline-item">
            <div class="timeline-icon interview">🎤</div>
            <div class="timeline-content">
                <div class="timeline-text">ראיון ${i.type} ${i.interviewer ? '- ' + i.interviewer : ''} ${i.completed ? '✅' : ''}</div>
                <div class="timeline-date">${formatDateTime(i.scheduledAt)}</div>
            </div>
        </div>`
    }));

    notes.forEach(n => items.push({
        date: n.createdAt,
        html: `<div class="timeline-item">
            <div class="timeline-icon note">📝</div>
            <div class="timeline-content">
                <div class="timeline-text">${n.content.substring(0, 100)}${n.content.length > 100 ? '...' : ''}</div>
                <div class="timeline-date">${formatDateTime(n.createdAt)} ${n.category ? '| ' + (NOTE_CATEGORIES_HE[n.category] || n.category) : ''}</div>
            </div>
        </div>`
    }));

    items.sort((a, b) => new Date(b.date) - new Date(a.date));
    return items.length > 0 ? items.map(i => i.html).join('') : '<p class="empty-state">אין פעילות עדיין</p>';
}

function renderInterviewCard(i) {
    return `<div class="item-card">
        <div class="item-header">
            <span class="item-title">ראיון ${i.type} ${i.completed ? '✅' : ''}</span>
            <div>
                <button class="btn btn-sm btn-secondary" onclick="showEditInterviewModal('${i.id}', ${JSON.stringify(i).replace(/"/g, '&quot;')})">ערוך</button>
                <button class="btn btn-sm btn-danger" onclick="deleteInterview('${i.id}', '${i.applicationId}')">מחק</button>
            </div>
        </div>
        <div class="item-meta">${formatDateTime(i.scheduledAt)} ${i.interviewer ? '| ' + i.interviewer : ''}</div>
        ${i.topics ? `<div class="item-body mt-1">נושאים: ${i.topics}</div>` : ''}
        ${i.notes ? `<div class="item-body">הערות: ${i.notes}</div>` : ''}
        ${i.feedback ? `<div class="item-body">פידבק: ${i.feedback}</div>` : ''}
    </div>`;
}

function renderNoteCard(n) {
    return `<div class="item-card">
        <div class="item-header">
            <span class="item-title">${n.category ? (NOTE_CATEGORIES_HE[n.category] || n.category) : 'הערה'}</span>
            <div>
                <button class="btn btn-sm btn-danger" onclick="deleteNote('${n.id}', '${n.applicationId}')">מחק</button>
            </div>
        </div>
        <div class="item-meta">${formatDateTime(n.createdAt)}</div>
        <div class="item-body mt-1" style="white-space:pre-wrap">${n.content}</div>
    </div>`;
}

// ============================================================
// MODALS
// ============================================================

function showModal(html) {
    document.getElementById('modal-content').innerHTML = html;
    document.getElementById('modal-overlay').classList.remove('hidden');
}

function closeModal() {
    document.getElementById('modal-overlay').classList.add('hidden');
}

// Close on overlay click
document.addEventListener('click', (e) => {
    if (e.target.id === 'modal-overlay') closeModal();
});

// STATUS UPDATE MODAL
function showStatusModal(appId, currentStatus) {
    let options = Object.entries(STATUS_HE)
        .map(([val, label]) => `<option value="${val}" ${val === currentStatus ? 'selected' : ''}>${label}</option>`)
        .join('');

    showModal(`
        <h3>עדכון סטטוס</h3>
        <div class="form-group">
            <label>סטטוס חדש</label>
            <select id="modal-status">${options}</select>
        </div>
        <div class="form-group">
            <label>הערה (אופציונלי)</label>
            <input type="text" id="modal-status-note" placeholder="הערה לשינוי הסטטוס">
        </div>
        <div class="btn-group">
            <button class="btn btn-primary" onclick="updateStatus('${appId}')">עדכן</button>
            <button class="btn btn-secondary" onclick="closeModal()">ביטול</button>
        </div>
    `);
}

async function updateStatus(appId) {
    try {
        await api(`/applications/${appId}/status`, {
            method: 'PUT',
            body: JSON.stringify({
                newStatus: document.getElementById('modal-status').value,
                note: document.getElementById('modal-status-note').value || null
            })
        });
        closeModal();
        showDetail(appId);
    } catch (e) {
        alert('שגיאה: ' + e.message);
    }
}

// INTERVIEW MODAL
function showInterviewModal(appId) {
    const typeOptions = INTERVIEW_TYPES.map(t => `<option value="${t}">${t}</option>`).join('');

    showModal(`
        <h3>הוסף ראיון</h3>
        <div class="form-group">
            <label>סוג ראיון</label>
            <select id="modal-int-type">${typeOptions}</select>
        </div>
        <div class="form-group">
            <label>תאריך ושעה</label>
            <input type="datetime-local" id="modal-int-date">
        </div>
        <div class="form-group">
            <label>מראיין/ת</label>
            <input type="text" id="modal-int-interviewer" placeholder="שם">
        </div>
        <div class="form-group">
            <label>נושאים</label>
            <input type="text" id="modal-int-topics" placeholder="נושאים לראיון">
        </div>
        <div class="btn-group">
            <button class="btn btn-primary" onclick="addInterview('${appId}')">הוסף</button>
            <button class="btn btn-secondary" onclick="closeModal()">ביטול</button>
        </div>
    `);
}

async function addInterview(appId) {
    try {
        await api(`/applications/${appId}/interviews`, {
            method: 'POST',
            body: JSON.stringify({
                type: document.getElementById('modal-int-type').value,
                scheduledAt: new Date(document.getElementById('modal-int-date').value).toISOString(),
                interviewer: document.getElementById('modal-int-interviewer').value || null,
                topics: document.getElementById('modal-int-topics').value || null,
                completed: false
            })
        });
        closeModal();
        showDetail(appId);
    } catch (e) {
        alert('שגיאה: ' + e.message);
    }
}

function showEditInterviewModal(intId, interview) {
    const typeOptions = INTERVIEW_TYPES.map(t =>
        `<option value="${t}" ${t === interview.type ? 'selected' : ''}>${t}</option>`
    ).join('');

    const dt = interview.scheduledAt ? new Date(interview.scheduledAt).toISOString().slice(0, 16) : '';

    showModal(`
        <h3>ערוך ראיון</h3>
        <div class="form-group">
            <label>סוג ראיון</label>
            <select id="modal-int-type">${typeOptions}</select>
        </div>
        <div class="form-group">
            <label>תאריך ושעה</label>
            <input type="datetime-local" id="modal-int-date" value="${dt}">
        </div>
        <div class="form-group">
            <label>מראיין/ת</label>
            <input type="text" id="modal-int-interviewer" value="${interview.interviewer || ''}">
        </div>
        <div class="form-group">
            <label>נושאים</label>
            <input type="text" id="modal-int-topics" value="${interview.topics || ''}">
        </div>
        <div class="form-group">
            <label>הערות</label>
            <textarea id="modal-int-notes">${interview.notes || ''}</textarea>
        </div>
        <div class="form-group">
            <label>פידבק</label>
            <textarea id="modal-int-feedback">${interview.feedback || ''}</textarea>
        </div>
        <div class="form-group">
            <label>
                <input type="checkbox" id="modal-int-completed" ${interview.completed ? 'checked' : ''}>
                הושלם
            </label>
        </div>
        <div class="btn-group">
            <button class="btn btn-primary" onclick="updateInterview('${intId}', '${interview.applicationId}')">שמור</button>
            <button class="btn btn-secondary" onclick="closeModal()">ביטול</button>
        </div>
    `);
}

async function updateInterview(intId, appId) {
    try {
        await api(`/interviews/${intId}`, {
            method: 'PUT',
            body: JSON.stringify({
                type: document.getElementById('modal-int-type').value,
                scheduledAt: new Date(document.getElementById('modal-int-date').value).toISOString(),
                interviewer: document.getElementById('modal-int-interviewer').value || null,
                topics: document.getElementById('modal-int-topics').value || null,
                notes: document.getElementById('modal-int-notes').value || null,
                feedback: document.getElementById('modal-int-feedback').value || null,
                completed: document.getElementById('modal-int-completed').checked
            })
        });
        closeModal();
        showDetail(appId);
    } catch (e) {
        alert('שגיאה: ' + e.message);
    }
}

async function deleteInterview(intId, appId) {
    if (!confirm('למחוק את הראיון?')) return;
    await api(`/interviews/${intId}`, { method: 'DELETE' });
    showDetail(appId);
}

// NOTE MODAL
function showNoteModal(appId) {
    const catOptions = NOTE_CATEGORIES.map(c =>
        `<option value="${c}">${NOTE_CATEGORIES_HE[c] || c}</option>`
    ).join('');

    showModal(`
        <h3>הוסף הערה</h3>
        <div class="form-group">
            <label>קטגוריה</label>
            <select id="modal-note-cat">${catOptions}</select>
        </div>
        <div class="form-group">
            <label>תוכן</label>
            <textarea id="modal-note-content" placeholder="כתוב הערה..."></textarea>
        </div>
        <div class="btn-group">
            <button class="btn btn-primary" onclick="addNote('${appId}')">הוסף</button>
            <button class="btn btn-secondary" onclick="closeModal()">ביטול</button>
        </div>
    `);
}

async function addNote(appId) {
    try {
        await api(`/applications/${appId}/notes`, {
            method: 'POST',
            body: JSON.stringify({
                content: document.getElementById('modal-note-content').value,
                category: document.getElementById('modal-note-cat').value
            })
        });
        closeModal();
        showDetail(appId);
    } catch (e) {
        alert('שגיאה: ' + e.message);
    }
}

async function deleteNote(noteId, appId) {
    if (!confirm('למחוק את ההערה?')) return;
    await api(`/notes/${noteId}`, { method: 'DELETE' });
    showDetail(appId);
}

// ============================================================
// DELETE APPLICATION
// ============================================================

async function deleteApplication(id) {
    if (!confirm('למחוק את המשרה? כל הראיונות וההערות ימחקו גם.')) return;
    await api(`/applications/${id}`, { method: 'DELETE' });
    showTab('list');
}

// ============================================================
// STATISTICS
// ============================================================

async function loadStats() {
    try {
        const stats = await api('/stats');

        document.getElementById('stats-summary').innerHTML = `
            <div class="summary-card">
                <div class="value">${stats.total}</div>
                <div class="label">סה"כ משרות</div>
            </div>
            <div class="summary-card">
                <div class="value">${stats.applied}</div>
                <div class="label">הוגשו</div>
            </div>
            <div class="summary-card">
                <div class="value">${stats.avgScore || '-'}</div>
                <div class="label">ציון ממוצע</div>
            </div>
            <div class="summary-card">
                <div class="value">${stats.responseRate}%</div>
                <div class="label">אחוז מענה</div>
            </div>
        `;

        const breakdown = stats.statusBreakdown || {};
        const max = Math.max(...Object.values(breakdown), 1);
        const barColors = {
            Analyzing: 'var(--yellow)',
            DecidedToApply: 'var(--purple)',
            Applied: 'var(--blue)',
            PhoneScreen: 'var(--green)',
            TechnicalInterview: 'var(--green)',
            FinalRound: 'var(--green)',
            OfferReceived: '#6ee7b7',
            Accepted: 'var(--green)',
            Rejected: 'var(--red)',
            Withdrawn: '#9ca3af'
        };

        document.getElementById('stats-breakdown').innerHTML = Object.entries(STATUS_HE).map(([key, label]) => {
            const count = breakdown[key] || 0;
            const pct = (count / max * 100).toFixed(0);
            const color = barColors[key] || 'var(--accent)';
            return `<div class="stat-bar-row">
                <span class="stat-bar-label">${label}</span>
                <div class="stat-bar-bg">
                    <div class="stat-bar-fill" style="width:${pct}%;background:${color}">${count > 0 ? count : ''}</div>
                </div>
                <span class="stat-bar-count">${count}</span>
            </div>`;
        }).join('');
    } catch (e) {
        console.error('Stats error:', e);
    }
}

// ============================================================
// AI ANALYSIS RENDERING
// ============================================================

function renderAnalysis(matchAnalysisJson) {
    if (!matchAnalysisJson) return '';

    let a;
    try {
        a = typeof matchAnalysisJson === 'string' ? JSON.parse(matchAnalysisJson) : matchAnalysisJson;
    } catch {
        return '';
    }

    const b = a.breakdown;
    const r = a.recommendation;

    function aBarColor(score, max) {
        const pct = score / max;
        if (pct >= 0.6) return 'fill-green';
        if (pct >= 0.4) return 'fill-yellow';
        return 'fill-red';
    }

    function aScoreBar(label, score, max) {
        if (score == null || max == null) return '';
        const pct = (score / max * 100).toFixed(0);
        return `<div class="analysis-score-bar">
            <span class="analysis-bar-label">${label}</span>
            <div class="analysis-bar-bg"><div class="analysis-bar-fill ${aBarColor(score, max)}" style="width:${pct}%"></div></div>
            <span class="analysis-bar-num">${score}/${max}</span>
        </div>`;
    }

    function aList(items, cls) {
        if (!items || items.length === 0) return '<p style="font-size:0.8rem;color:var(--text-dim);margin:0.25rem 0">-</p>';
        return '<ul class="analysis-list">' + items.map(i => `<li class="${cls || ''}">${i}</li>`).join('') + '</ul>';
    }

    const verdictClass = a.verdict ? a.verdict.replace(/ /g, '_') : 'INSUFFICIENT_DATA';
    const verdictLabel = (a.verdict || 'INSUFFICIENT DATA').replace(/_/g, ' ');

    let html = `<div class="card analysis-card">
        <div class="analysis-header" onclick="toggleCollapse(this)">
            <h3 class="section-title" style="border:none;margin:0;padding:0">ניתוח AI</h3>
        </div>
        <div class="analysis-body">
            <div style="display:flex;justify-content:space-between;align-items:flex-start;flex-wrap:wrap;gap:1rem">
                <div>
                    <div class="analysis-verdict ${verdictClass}">${verdictLabel}</div>
                    <div class="analysis-overall-score">ציון כללי: ${a.overallScore ?? 'N/A'} / 100</div>
                </div>
            </div>`;

    if (b) {
        html += `<div class="analysis-section">
            <h4>ציונים</h4>
            ${b.technical ? aScoreBar('טכני', b.technical.score, b.technical.maxScore) : ''}
            ${b.cultural ? aScoreBar('תרבותי', b.cultural.score, b.cultural.maxScore) : ''}
            ${b.roleCharacteristics ? aScoreBar('התאמה לתפקיד', b.roleCharacteristics.score, b.roleCharacteristics.maxScore) : ''}
        </div>`;

        if (b.technical) {
            html += `<div class="analysis-section">
                <h4>טכני</h4>
                <div class="analysis-sub-label">חוזקות</div>
                ${aList(b.technical.strengths, 'a-positive')}
                <div class="analysis-sub-label">פערים</div>
                ${aList(b.technical.gaps, 'a-negative')}
            </div>`;
        }

        if (b.cultural) {
            html += `<div class="analysis-section">
                <h4>תרבות</h4>
                <div class="analysis-sub-label">סימנים חיוביים</div>
                ${aList(b.cultural.positiveSignals, 'a-positive')}
                <div class="analysis-sub-label">חששות</div>
                ${aList(b.cultural.concerns, 'a-negative')}
            </div>`;
        }

        if (b.roleCharacteristics) {
            html += `<div class="analysis-section">
                <h4>מאפייני התפקיד</h4>
                <div class="analysis-sub-label">הזדמנויות</div>
                ${aList(b.roleCharacteristics.opportunities, 'a-positive')}
                <div class="analysis-sub-label">סיכונים</div>
                ${aList(b.roleCharacteristics.risks, 'a-negative')}
            </div>`;
        }
    }

    if (r) {
        html += `<div class="analysis-section">
            <h4>המלצה</h4>
            <div class="analysis-should-apply ${r.shouldApply ? 'yes' : 'no'}">
                ${r.shouldApply ? 'כדאי להגיש' : 'לא כדאי להגיש'}
            </div>
            <div class="analysis-sub-label">סיבות עיקריות</div>
            ${aList(r.keyReasons)}
            <div class="analysis-sub-label">שאלות לשאול</div>
            ${aList(r.questionsToAsk)}
            <div class="analysis-flags">
                ${(r.greenFlags || []).map(f => `<span class="analysis-flag flag-green">${f}</span>`).join('')}
                ${(r.redFlags || []).map(f => `<span class="analysis-flag flag-red">${f}</span>`).join('')}
            </div>
        </div>`;
    }

    if (a.honestAssessment) {
        html += `<div class="analysis-section">
            <h4>הערכה כנה</h4>
            <div class="analysis-assessment">${a.honestAssessment}</div>
        </div>`;
    }

    html += `</div></div>`;
    return html;
}

// ============================================================
// COLLAPSIBLE
// ============================================================

function toggleCollapse(header) {
    header.classList.toggle('collapsed');
    const body = header.nextElementSibling;
    body.classList.toggle('hidden');
}
