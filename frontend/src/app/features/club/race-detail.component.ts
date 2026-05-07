import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe, DecimalPipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { AuthService, LiveRaceService } from '../../core/services/services';
import { Race, RaceResult, RaceStatus } from '../../core/models';

@Component({
  selector: 'app-race-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, NgClass, FormsModule],
  template: `
    <!-- Back nav -->
    <div class="flex items-center gap-3 mb-6">
      <a routerLink="/club/races" class="pr-btn pr-btn--ghost pr-btn--sm">← Races</a>
      <span class="text-muted">/</span>
      <span class="text-sm">{{ race()?.name }}</span>
    </div>

    @if (!race()) {
      <div class="pr-empty"><div class="pr-spinner" style="margin:0 auto"></div></div>
    } @else {
      <!-- Header -->
      <div class="pr-page-header flex justify-between items-center">
        <div>
          <div class="flex items-center gap-3 mb-2">
            <h1 class="pr-page-header__title" style="margin:0">{{ race()!.name }}</h1>
            <span [class]="'pr-badge ' + statusBadge(race()!.status)">{{ statusLabel(race()!.status) }}</span>
            @if (race()!.isLiveTracking) {
              <span class="live-indicator">
                <span class="live-dot"></span> LIVE
              </span>
            }
          </div>
          <p class="pr-page-header__subtitle">
            📍 {{ race()!.releaseLocation }} ·
            @if (race()!.actualReleaseTime) {
              Released {{ race()!.actualReleaseTime | date:'dd MMM yyyy HH:mm' }}
            } @else {
              Scheduled {{ race()!.scheduledReleaseTime | date:'dd MMM yyyy HH:mm' }}
            }
          </p>
        </div>

        <!-- Race action buttons -->
        <div class="flex gap-3">
          @if (race()!.status === RaceStatus.Draft || race()!.status === RaceStatus.Scheduled) {
            <button class="pr-btn pr-btn--primary" (click)="startRace()">▶ Start Race</button>
          }
          @if (race()!.status === RaceStatus.InProgress) {
            <button class="pr-btn pr-btn--outline" (click)="completeRace()">⏹ Complete</button>
          }
          @if (race()!.status === RaceStatus.Completed) {
            <button class="pr-btn pr-btn--primary" (click)="processResults()">⚙ Process Rankings</button>
            <button class="pr-btn pr-btn--outline" (click)="publishRace()">📢 Publish</button>
          }
        </div>
      </div>

      <!-- Race info bar -->
      <div class="pr-card mb-6 race-info-bar">
        @if (race()!.windSpeedKmh) {
          <div class="info-item"><span class="info-label">Wind</span><span>{{ race()!.windSpeedKmh }} km/h {{ race()!.windDirection }}</span></div>
        }
        @if (race()!.temperatureCelsius) {
          <div class="info-item"><span class="info-label">Temp</span><span>{{ race()!.temperatureCelsius }}°C</span></div>
        }
        <div class="info-item"><span class="info-label">Entries</span><span>{{ race()!.totalPigeonsEntered ?? results().length }}</span></div>
        @for (cat of race()!.categories; track cat.id) {
          <div class="info-item"><span class="info-label">Category</span><span>{{ cat.name }}</span></div>
        }
      </div>

      <!-- Tabs -->
      <div class="editor-tabs mb-6">
        @for (t of tabs; track t.id) {
          <button class="editor-tab" [class.editor-tab--active]="activeTab() === t.id" (click)="activeTab.set(t.id)">
            {{ t.icon }} {{ t.label }}
          </button>
        }
      </div>

      <!-- Results tab -->
      @if (activeTab() === 'results') {
        <div class="pr-card">
          <div class="flex justify-between items-center mb-4">
            <h3 style="margin:0">Results ({{ results().length }})</h3>
            <div class="flex gap-2">
              @if (canEdit()) {
                <button class="pr-btn pr-btn--ghost pr-btn--sm" (click)="activeTab.set('ingest')">📥 Upload ETS</button>
                <button class="pr-btn pr-btn--ghost pr-btn--sm" (click)="showManualForm.set(true)">+ Manual Entry</button>
              }
            </div>
          </div>

          @if (showManualForm()) {
            <div class="manual-form mb-6">
              <h4 style="margin-bottom:16px">Manual Result Entry</h4>
              <div class="pr-grid-3" style="gap:12px;margin-bottom:12px">
                <div class="pr-form-group">
                  <label class="pr-label">Ring Number *</label>
                  <input class="pr-input" [(ngModel)]="manual.ringNumber" placeholder="BE24-1234567">
                </div>
                <div class="pr-form-group">
                  <label class="pr-label">Arrival Time *</label>
                  <input class="pr-input" type="datetime-local" [(ngModel)]="manual.arrivalTime">
                </div>
                <div class="pr-form-group">
                  <label class="pr-label">Category</label>
                  <select class="pr-select" [(ngModel)]="manual.categoryId">
                    <option value="">No Category</option>
                    @for (cat of race()!.categories; track cat.id) {
                      <option [value]="cat.id">{{ cat.name }}</option>
                    }
                  </select>
                </div>
                <div class="pr-form-group">
                  <label class="pr-label">Pigeon Name</label>
                  <input class="pr-input" [(ngModel)]="manual.pigeonName" placeholder="Optional">
                </div>
                <div class="pr-form-group">
                  <label class="pr-label">Sex</label>
                  <select class="pr-select" [(ngModel)]="manual.pigeonSex">
                    <option value="">Unknown</option>
                    <option value="M">Male (Cock)</option>
                    <option value="F">Female (Hen)</option>
                  </select>
                </div>
                <div class="pr-form-group">
                  <label class="pr-label">Year of Birth</label>
                  <input class="pr-input" type="number" [(ngModel)]="manual.pigeonYearOfBirth" placeholder="{{ currentYear }}">
                </div>
              </div>
              <div class="flex gap-3">
                <button class="pr-btn pr-btn--primary" [disabled]="saving()" (click)="submitManual()">
                  @if (saving()) { <span class="pr-spinner" style="width:14px;height:14px"></span> }
                  Add Result
                </button>
                <button class="pr-btn pr-btn--ghost" (click)="showManualForm.set(false)">Cancel</button>
              </div>
            </div>
          }

          @if (results().length === 0) {
            <div class="pr-empty">
              <div class="pr-empty__icon">📋</div>
              <div class="pr-empty__title">No results yet</div>
              <p class="pr-empty__desc">Upload an ETS file or add results manually.</p>
            </div>
          } @else {
            <div class="pr-table-wrapper">
              <table class="pr-table">
                <thead>
                  <tr>
                    <th>Rank</th><th>Ring #</th><th>Pigeon</th><th>Fancier</th>
                    <th>Velocity</th><th>Distance</th><th>Arrival</th><th>Status</th>
                    @if (canEdit()) { <th></th> }
                  </tr>
                </thead>
                <tbody>
                  @for (r of results(); track r.id) {
                    <tr [class.result-invalid]="r.isDuplicate || r.hasInvalidTimestamp">
                      <td><span [class]="'pr-rank ' + rankClass(r.clubRank)">{{ r.clubRank ?? '—' }}</span></td>
                      <td><code style="font-size:0.8rem;background:var(--pr-surface-2);padding:2px 6px;border-radius:4px">{{ r.ringNumber }}</code></td>
                      <td>{{ r.pigeonName ?? '—' }} <span class="text-muted text-sm">{{ r.pigeonSex }}</span></td>
                      <td class="text-muted">{{ r.fancierName ?? '—' }}</td>
                      <td class="font-bold">{{ r.velocityMperMin | number:'1.0-1' }} <span class="text-muted text-sm">m/min</span></td>
                      <td>{{ r.distanceKm | number:'1.3-3' }} km</td>
                      <td class="text-muted text-sm">{{ r.arrivalTime | date:'HH:mm:ss' }}</td>
                      <td>
                        @if (r.isDuplicate) { <span class="pr-badge pr-badge--warning">Duplicate</span> }
                        @else if (r.hasInvalidTimestamp) { <span class="pr-badge pr-badge--error">Invalid TS</span> }
                        @else if (r.isLateArrival) { <span class="pr-badge pr-badge--warning">Late</span> }
                        @else { <span [class]="'pr-badge ' + resultStatusBadge(r.status)">{{ resultStatusLabel(r.status) }}</span> }
                      </td>
                      @if (canEdit()) {
                        <td>
                          <button class="pr-btn pr-btn--danger pr-btn--sm" (click)="deleteResult(r.id)">✕</button>
                        </td>
                      }
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </div>
      }

      <!-- ETS Upload tab -->
      @if (activeTab() === 'ingest') {
        <div class="pr-card" style="max-width:560px">
          <h3 style="margin-bottom:20px">Upload ETS File</h3>

          <div class="drop-zone"
               [class.drop-zone--active]="dragOver()"
               (dragover)="dragOver.set(true); $event.preventDefault()"
               (dragleave)="dragOver.set(false)"
               (drop)="onDrop($event)">
            <div class="drop-zone__icon">📁</div>
            <div class="drop-zone__text">Drop ETS / Excel / CSV here</div>
            <div class="drop-zone__sub">or</div>
            <label class="pr-btn pr-btn--outline pr-btn--sm">
              Browse Files
              <input type="file" style="display:none" accept=".xlsx,.xls,.csv" (change)="onFileSelect($event)">
            </label>
          </div>

          @if (selectedFile()) {
            <div class="file-preview mt-4">
              <span>📄 {{ selectedFile()!.name }}</span>
              <span class="text-muted text-sm">{{ (selectedFile()!.size / 1024).toFixed(1) }} KB</span>
            </div>
          }

          @if (race()!.categories.length > 0) {
            <div class="pr-form-group mt-4">
              <label class="pr-label">Assign to Category (Optional)</label>
              <select class="pr-select" [(ngModel)]="ingestCategoryId">
                <option value="">No Category</option>
                @for (cat of race()!.categories; track cat.id) {
                  <option [value]="cat.id">{{ cat.name }}</option>
                }
              </select>
            </div>
          }

          <button class="pr-btn pr-btn--primary mt-4"
                  [disabled]="!selectedFile() || uploading()"
                  (click)="uploadFile()">
            @if (uploading()) { <span class="pr-spinner" style="width:14px;height:14px"></span> Uploading... }
            @else { 📤 Upload & Parse }
          </button>

          @if (ingestResult()) {
            <div class="pr-alert pr-alert--success mt-4">
              ✓ Parsed {{ ingestResult()!.successfulRows }} results
              ({{ ingestResult()!.failedRows }} failed, {{ ingestResult()!.duplicateRows }} duplicates)
            </div>
          }
        </div>
      }

      <!-- Ingestion Logs tab -->
      @if (activeTab() === 'logs') {
        <div class="pr-card">
          <h3 style="margin-bottom:20px">Ingestion Logs</h3>
          @if (logs().length === 0) {
            <div class="pr-empty">
              <div class="pr-empty__icon">📋</div>
              <div class="pr-empty__title">No ingestion logs</div>
            </div>
          } @else {
            <div class="pr-table-wrapper">
              <table class="pr-table">
                <thead><tr><th>File</th><th>Type</th><th>Total</th><th>Success</th><th>Failed</th><th>Duplicates</th><th>Date</th><th>Status</th></tr></thead>
                <tbody>
                  @for (l of logs(); track l.id) {
                    <tr>
                      <td class="font-bold text-sm">{{ l.fileName }}</td>
                      <td><span class="pr-badge pr-badge--info">{{ l.ingestionType }}</span></td>
                      <td>{{ l.totalRowsRead }}</td>
                      <td class="text-sm" style="color:var(--pr-success)">{{ l.successfulRows }}</td>
                      <td class="text-sm" style="color:var(--pr-error)">{{ l.failedRows }}</td>
                      <td class="text-sm" style="color:var(--pr-warning)">{{ l.duplicateRows }}</td>
                      <td class="text-muted text-sm">{{ l.processedAt | date:'dd MMM HH:mm' }}</td>
                      <td><span [class]="l.isSuccess ? 'pr-badge pr-badge--success' : 'pr-badge pr-badge--error'">{{ l.isSuccess ? 'OK' : 'Error' }}</span></td>
                    </tr>
                  }
                </tbody>
              </table>
            </div>
          }
        </div>
      }
    }
  `,
  styles: [`
    .live-indicator {
      display:inline-flex; align-items:center; gap:6px;
      font-family:var(--font-display); font-size:0.75rem; font-weight:700;
      color:var(--pr-success); letter-spacing:0.08em;
    }
    .live-dot {
      width:8px; height:8px; border-radius:50%;
      background:var(--pr-success); animation:pulse 1.5s ease-in-out infinite;
    }
    .race-info-bar { display:flex; gap:32px; flex-wrap:wrap; padding:16px 24px; }
    .info-item { display:flex; flex-direction:column; gap:2px; }
    .info-label { font-size:0.7rem; text-transform:uppercase; letter-spacing:0.06em; color:var(--pr-text-muted); }

    .editor-tabs { display:flex; gap:4px; border-bottom:1px solid var(--pr-border); }
    .editor-tab {
      padding:10px 20px; border:none; background:transparent;
      color:var(--pr-text-muted); font-family:var(--font-body); font-size:0.875rem;
      cursor:pointer; border-bottom:2px solid transparent; margin-bottom:-1px;
      transition:all var(--t-fast);
    }
    .editor-tab:hover { color:var(--pr-text); }
    .editor-tab--active { color:var(--pr-primary); border-bottom-color:var(--pr-primary); }

    .manual-form {
      background:var(--pr-surface-2); border-radius:var(--pr-radius);
      padding:20px; border:1px solid var(--pr-border);
    }

    .result-invalid { opacity:0.6; }

    .drop-zone {
      border:2px dashed var(--pr-border); border-radius:var(--pr-radius);
      padding:40px 24px; text-align:center; transition:all var(--t-base); cursor:pointer;
    }
    .drop-zone--active { border-color:var(--pr-primary); background:rgba(30,144,255,0.04); }
    .drop-zone:hover { border-color:var(--pr-primary); }
    .drop-zone__icon { font-size:2.5rem; margin-bottom:12px; }
    .drop-zone__text { font-weight:600; margin-bottom:4px; }
    .drop-zone__sub { color:var(--pr-text-muted); font-size:0.875rem; margin-bottom:12px; }

    .file-preview {
      display:flex; justify-content:space-between; align-items:center;
      background:var(--pr-surface-2); padding:10px 16px; border-radius:var(--pr-radius);
      border:1px solid var(--pr-border);
    }
  `]
})
export class RaceDetailComponent implements OnInit, OnDestroy {
  private route   = inject(ActivatedRoute);
  private api     = inject(ApiService);
  private liveRace = inject(LiveRaceService);
  auth = inject(AuthService);

  RaceStatus = RaceStatus;
  race       = signal<Race | null>(null);
  results    = signal<RaceResult[]>([]);
  logs       = signal<any[]>([]);
  activeTab  = signal('results');
  saving     = signal(false);
  uploading  = signal(false);
  dragOver   = signal(false);
  showManualForm = signal(false);
  selectedFile   = signal<File | null>(null);
  ingestResult   = signal<any | null>(null);
  ingestCategoryId = '';
  currentYear = new Date().getFullYear();

  manual = { ringNumber: '', arrivalTime: '', categoryId: '', pigeonName: '', pigeonSex: '', pigeonYearOfBirth: undefined as number | undefined };

  tabs = [
    { id: 'results', icon: '📋', label: 'Results' },
    { id: 'ingest',  icon: '📥', label: 'Upload ETS' },
    { id: 'logs',    icon: '📊', label: 'Ingestion Logs' },
  ];

  canEdit = () => this.auth.canManageRaces() && this.race()?.status !== RaceStatus.Published;

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.api.getRace(id).subscribe(r => {
      this.race.set(r);
      if (r.isLiveTracking) this.connectLive(id);
    });
    this.api.getRaceResults(id, 1, 200).subscribe(p => this.results.set(p.items as RaceResult[]));
    this.api.getIngestionLogs(id).subscribe(l => this.logs.set(l));
  }

  ngOnDestroy() { this.liveRace.disconnect(); }

  connectLive(raceId: string) {
    this.liveRace.connect();
    this.liveRace.joinRace(raceId);
    this.liveRace.onNewResult((r: RaceResult) => {
      this.results.update(arr => [r, ...arr]);
    });
    this.liveRace.onRaceStatusChanged((data) => {
      this.race.update(r => r ? { ...r, status: data.status as any } : r);
    });
  }

  startRace() {
    const id = this.race()!.id;
    const now = new Date().toISOString();
    this.api.startRace(id, now).subscribe(r => this.race.set(r));
  }

  completeRace() {
    this.api.completeRace(this.race()!.id).subscribe(r => this.race.set(r));
  }

  processResults() {
    this.api.processResults(this.race()!.id).subscribe(() => {
      this.api.getRaceResults(this.race()!.id, 1, 200).subscribe(p => this.results.set(p.items as RaceResult[]));
    });
  }

  publishRace() {
    this.api.publishRace(this.race()!.id).subscribe(r => this.race.set(r));
  }

  submitManual() {
    this.saving.set(true);
    this.api.addManualResult({
      raceId: this.race()!.id,
      ringNumber: this.manual.ringNumber,
      arrivalTime: this.manual.arrivalTime,
      categoryId: this.manual.categoryId || undefined,
      pigeonName: this.manual.pigeonName || undefined,
      pigeonSex: this.manual.pigeonSex || undefined,
      pigeonYearOfBirth: this.manual.pigeonYearOfBirth,
    }).subscribe({
      next: r => {
        this.results.update(arr => [...arr, r]);
        this.manual = { ringNumber: '', arrivalTime: '', categoryId: '', pigeonName: '', pigeonSex: '', pigeonYearOfBirth: undefined };
        this.showManualForm.set(false);
        this.saving.set(false);
      },
      error: () => this.saving.set(false)
    });
  }

  deleteResult(id: string) {
    this.api.deleteResult(id).subscribe(() => {
      this.results.update(arr => arr.filter(r => r.id !== id));
    });
  }

  onFileSelect(e: Event) {
    const f = (e.target as HTMLInputElement).files?.[0];
    if (f) this.selectedFile.set(f);
  }

  onDrop(e: DragEvent) {
    e.preventDefault();
    this.dragOver.set(false);
    const f = e.dataTransfer?.files?.[0];
    if (f) this.selectedFile.set(f);
  }

  uploadFile() {
    const file = this.selectedFile();
    if (!file) return;
    this.uploading.set(true);
    this.api.ingestETSFile(this.race()!.id, this.ingestCategoryId || null, file)
      .subscribe({
        next: result => {
          this.ingestResult.set(result);
          this.uploading.set(false);
          this.api.getRaceResults(this.race()!.id, 1, 200).subscribe(p => this.results.set(p.items as RaceResult[]));
          this.api.getIngestionLogs(this.race()!.id).subscribe(l => this.logs.set(l));
        },
        error: () => this.uploading.set(false)
      });
  }

  statusBadge(s: RaceStatus) {
    const m: Record<number, string> = { 1:'pr-badge--muted', 2:'pr-badge--info', 3:'pr-badge--warning', 4:'pr-badge--info', 5:'pr-badge--success', 6:'pr-badge--error' };
    return m[s] ?? 'pr-badge--muted';
  }
  statusLabel(s: RaceStatus) { return RaceStatus[s]; }

  resultStatusBadge(s: number) {
    const m: Record<number, string> = { 1:'pr-badge--muted', 2:'pr-badge--info', 3:'pr-badge--success', 4:'pr-badge--error' };
    return m[s] ?? 'pr-badge--muted';
  }
  resultStatusLabel(s: number) { return ['','Pending','Validated','Published','Rejected'][s] ?? ''; }

  rankClass(rank?: number | null) {
    if (!rank) return 'pr-rank--other';
    return rank <= 3 ? `pr-rank--${rank}` : 'pr-rank--other';
  }
}
