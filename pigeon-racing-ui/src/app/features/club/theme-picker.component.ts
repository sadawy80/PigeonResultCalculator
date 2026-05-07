import { Component, inject, signal, OnInit, output, input } from '@angular/core';
import { NgClass, NgStyle } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { ThemeService, AuthService } from '../../core/services/services';
import { Theme, SiteTheme } from '../../core/models';

// ── Theme Picker Component ────────────────────────────────────────────────────

@Component({
  selector: 'app-theme-picker',
  standalone: true,
  imports: [NgClass, NgStyle],
  template: `
    <div class="theme-picker">
      <h3 class="theme-picker__title">Choose Your Theme</h3>
      <p class="theme-picker__sub">Select a visual theme for your club's public page</p>

      <div class="theme-grid">
        @for (theme of themes(); track theme.id) {
          <button
            class="theme-card"
            [class.theme-card--active]="selectedTheme() === theme.id"
            (click)="select(theme.id)">

            <!-- Color preview bar -->
            <div class="theme-card__preview">
              <div class="theme-card__bar"
                   [style.background]="'linear-gradient(135deg,' + theme.primaryColor + ',' + theme.accentColor + ')'">
              </div>
              <div class="theme-card__surface" [style.background]="theme.surfaceColor">
                <div class="theme-card__dot" [style.background]="theme.primaryColor"></div>
                <div class="theme-card__line" [style.background]="theme.textColor + '33'"></div>
                <div class="theme-card__line theme-card__line--short" [style.background]="theme.textColor + '22'"></div>
              </div>
            </div>

            <div class="theme-card__info">
              <div class="theme-card__name">{{ theme.name }}</div>
              <div class="theme-card__desc">{{ theme.description }}</div>
            </div>

            @if (selectedTheme() === theme.id) {
              <div class="theme-card__check">✓</div>
            }
          </button>
        }
      </div>

      @if (clubId()) {
        <button class="pr-btn pr-btn--primary mt-6"
                [disabled]="saving()"
                (click)="save()">
          @if (saving()) { <span class="pr-spinner" style="width:16px;height:16px"></span> }
          Apply Theme
        </button>
      }
    </div>
  `,
  styles: [`
    .theme-picker { max-width: 800px; }
    .theme-picker__title { font-family: var(--font-display); font-size: 1.375rem; font-weight: 700; margin-bottom: 4px; }
    .theme-picker__sub { color: var(--pr-text-muted); font-size: 0.875rem; margin-bottom: 32px; }

    .theme-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
      gap: 16px;
    }

    .theme-card {
      position: relative;
      background: var(--pr-surface);
      border: 2px solid var(--pr-border);
      border-radius: calc(var(--pr-radius) * 1.5);
      overflow: hidden;
      cursor: pointer;
      transition: all var(--t-base);
      text-align: left;
      padding: 0;
    }
    .theme-card:hover { border-color: var(--pr-primary); transform: translateY(-3px); box-shadow: var(--shadow-md); }
    .theme-card--active { border-color: var(--pr-primary); box-shadow: 0 0 0 3px rgba(30,144,255,0.25); }

    .theme-card__preview {
      height: 100px;
      display: flex;
      flex-direction: column;
    }
    .theme-card__bar { height: 40px; }
    .theme-card__surface {
      flex: 1;
      padding: 8px 12px;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }
    .theme-card__dot { width: 24px; height: 24px; border-radius: 50%; margin-bottom: 4px; }
    .theme-card__line { height: 6px; border-radius: 3px; width: 80%; }
    .theme-card__line--short { width: 55%; }

    .theme-card__info { padding: 12px; }
    .theme-card__name { font-family: var(--font-display); font-weight: 700; font-size: 0.95rem; }
    .theme-card__desc { font-size: 0.75rem; color: var(--pr-text-muted); margin-top: 2px; line-height: 1.4; }

    .theme-card__check {
      position: absolute; top: 8px; right: 8px;
      width: 24px; height: 24px; border-radius: 50%;
      background: var(--pr-primary); color: #fff;
      display: flex; align-items: center; justify-content: center;
      font-size: 0.75rem; font-weight: 700;
    }
  `]
})
export class ThemePickerComponent implements OnInit {
  private api = inject(ApiService);
  themeService = inject(ThemeService);

  clubId = input<string | null>(null);
  themeApplied = output<SiteTheme>();

  themes    = signal<Theme[]>([]);
  selectedTheme = signal<SiteTheme>(this.themeService.activeTheme());
  saving    = signal(false);

  ngOnInit() {
    this.api.getThemes().subscribe(t => this.themes.set(t));
  }

  select(theme: SiteTheme) {
    this.selectedTheme.set(theme);
    this.themeService.applyTheme(theme); // live preview
  }

  save() {
    const cid = this.clubId();
    if (!cid) return;
    this.saving.set(true);
    this.api.setClubTheme(cid, this.selectedTheme()).subscribe({
      next: () => {
        this.saving.set(false);
        this.themeApplied.emit(this.selectedTheme());
      },
      error: () => this.saving.set(false)
    });
  }
}

// ── Club Page Editor ──────────────────────────────────────────────────────────

@Component({
  selector: 'app-club-page-editor',
  standalone: true,
  imports: [ThemePickerComponent, FormsModule, NgClass],
  template: `
    <div class="pr-page-header">
      <h1 class="pr-page-header__title">Club Page</h1>
      <p class="pr-page-header__subtitle">Customize your public-facing club page</p>
    </div>

    <!-- Tab navigation -->
    <div class="editor-tabs mb-6">
      @for (tab of tabs; track tab.id) {
        <button class="editor-tab"
                [class.editor-tab--active]="activeTab() === tab.id"
                (click)="activeTab.set(tab.id)">
          {{ tab.icon }} {{ tab.label }}
        </button>
      }
    </div>

    <!-- Theme tab -->
    @if (activeTab() === 'theme') {
      <app-theme-picker [clubId]="clubId" (themeApplied)="onThemeApplied($event)" />
    }

    <!-- Branding tab -->
    @if (activeTab() === 'branding') {
      <div class="pr-card" style="max-width:560px">
        <h3 style="margin-bottom:24px">Branding</h3>
        <div style="display:flex;flex-direction:column;gap:20px">
          <div class="pr-form-group">
            <label class="pr-label">Logo URL</label>
            <input class="pr-input" [(ngModel)]="branding.logoUrl" placeholder="https://...">
          </div>
          <div style="display:grid;grid-template-columns:1fr 1fr;gap:16px">
            <div class="pr-form-group">
              <label class="pr-label">Primary Color</label>
              <div class="flex gap-2">
                <input type="color" [(ngModel)]="branding.primaryColor" style="width:42px;height:42px;border:none;cursor:pointer;background:none">
                <input class="pr-input" [(ngModel)]="branding.primaryColor" placeholder="#1E90FF">
              </div>
            </div>
            <div class="pr-form-group">
              <label class="pr-label">Secondary Color</label>
              <div class="flex gap-2">
                <input type="color" [(ngModel)]="branding.secondaryColor" style="width:42px;height:42px;border:none;cursor:pointer;background:none">
                <input class="pr-input" [(ngModel)]="branding.secondaryColor" placeholder="#00D4FF">
              </div>
            </div>
          </div>
          <button class="pr-btn pr-btn--primary" [disabled]="saving()" (click)="saveBranding()">
            @if (saving()) { <span class="pr-spinner" style="width:16px;height:16px"></span> }
            Save Branding
          </button>
          @if (saveSuccess()) { <div class="pr-alert pr-alert--success">Saved successfully!</div> }
        </div>
      </div>
    }

    <!-- Announcements tab -->
    @if (activeTab() === 'announcements') {
      <div class="pr-card" style="max-width:640px">
        <h3 style="margin-bottom:24px">Announcements</h3>
        <textarea class="pr-textarea" rows="8"
                  [(ngModel)]="announcementsText"
                  placeholder="Write announcements for your club members..."></textarea>
        <button class="pr-btn pr-btn--primary mt-4">Save Announcements</button>
      </div>
    }

    <!-- URL tab -->
    @if (activeTab() === 'url') {
      <div class="pr-card" style="max-width:560px">
        <h3 style="margin-bottom:8px">Public Page URL</h3>
        <p style="color:var(--pr-text-muted);font-size:0.875rem;margin-bottom:24px">
          Shareable link for your club's public race results page.
        </p>
        @if (!slugEditing()) {
          <div class="slug-row">
            <div class="slug-display">
              <span class="slug-prefix">/p/</span>
              <span class="slug-value">{{ slug() }}</span>
            </div>
            <div class="slug-actions">
              <button class="pr-btn pr-btn--ghost" style="font-size:0.8rem" (click)="copyUrl()">📋 Copy</button>
              <button class="pr-btn pr-btn--outline" style="font-size:0.8rem" (click)="startEditSlug()">✏️ Edit</button>
            </div>
          </div>
          @if (slugCopied()) {
            <div class="pr-alert pr-alert--success mt-3">Link copied to clipboard!</div>
          }
        } @else {
          <div style="display:flex;flex-direction:column;gap:12px">
            <div class="pr-form-group">
              <label class="pr-label">Custom URL slug</label>
              <div class="slug-edit-row">
                <span class="slug-prefix-box">/p/</span>
                <input class="pr-input" style="border-radius:0 var(--pr-radius) var(--pr-radius) 0;border-left:none"
                       [(ngModel)]="slugEditValue" placeholder="my-club-name"
                       (input)="validateSlug()">
              </div>
              <p class="pr-form-hint" style="margin-top:6px;font-size:0.78rem;color:var(--pr-text-muted)">
                Lowercase letters, numbers, and hyphens only. Min 3 characters.
              </p>
              @if (slugError()) {
                <p style="color:var(--pr-danger,#e53e3e);font-size:0.8rem;margin-top:4px">{{ slugError() }}</p>
              }
            </div>
            <div style="display:flex;gap:8px">
              <button class="pr-btn pr-btn--primary" [disabled]="slugSaving() || !!slugError()" (click)="saveSlug()">
                @if (slugSaving()) { <span class="pr-spinner" style="width:16px;height:16px"></span> }
                Save
              </button>
              <button class="pr-btn pr-btn--ghost" (click)="cancelEditSlug()">Cancel</button>
            </div>
            @if (slugSuccess()) {
              <div class="pr-alert pr-alert--success">URL updated successfully!</div>
            }
            @if (slugConflict()) {
              <div class="pr-alert pr-alert--error">This URL is already taken. Please choose a different one.</div>
            }
          </div>
        }
      </div>
    }
  `,
  styles: [`
    .editor-tabs { display:flex; gap:4px; border-bottom:1px solid var(--pr-border); padding-bottom:0; }
    .editor-tab {
      padding:10px 20px; border:none; background:transparent;
      color:var(--pr-text-muted); font-family:var(--font-body); font-size:0.9rem;
      cursor:pointer; border-bottom:2px solid transparent; margin-bottom:-1px;
      transition:all var(--t-fast);
    }
    .editor-tab:hover { color:var(--pr-text); }
    .editor-tab--active { color:var(--pr-primary); border-bottom-color:var(--pr-primary); }

    .slug-row { display:flex; align-items:center; justify-content:space-between; gap:12px; padding:14px 16px; background:var(--pr-bg); border:1px solid var(--pr-border); border-radius:var(--pr-radius); }
    .slug-display { display:flex; align-items:center; gap:4px; font-family:var(--font-mono,monospace); font-size:0.9rem; }
    .slug-prefix { color:var(--pr-text-muted); }
    .slug-value { color:var(--pr-primary); font-weight:600; }
    .slug-actions { display:flex; gap:6px; flex-shrink:0; }
    .slug-edit-row { display:flex; align-items:stretch; }
    .slug-prefix-box { display:flex; align-items:center; padding:0 12px; background:var(--pr-bg); border:1px solid var(--pr-border); border-radius:var(--pr-radius) 0 0 var(--pr-radius); color:var(--pr-text-muted); font-size:0.875rem; white-space:nowrap; }
  `]
})
export class ClubPageEditorComponent implements OnInit {
  private api = inject(ApiService);
  themeService = inject(ThemeService);
  private auth = inject(AuthService);

  get clubId() { return this.auth.clubId() ?? ''; }
  activeTab = signal('theme');
  saving = signal(false);
  saveSuccess = signal(false);
  announcementsText = '';

  branding = { logoUrl: '', primaryColor: '#1E90FF', secondaryColor: '#00D4FF' };

  slug         = signal('');
  slugEditing  = signal(false);
  slugEditValue = '';
  slugSaving   = signal(false);
  slugError    = signal('');
  slugSuccess  = signal(false);
  slugConflict = signal(false);
  slugCopied   = signal(false);

  tabs = [
    { id: 'theme',         icon: '🎨', label: 'Theme' },
    { id: 'branding',      icon: '🏷️', label: 'Branding' },
    { id: 'announcements', icon: '📢', label: 'Announcements' },
    { id: 'url',           icon: '🔗', label: 'Page URL' },
  ];

  ngOnInit() {
    if (this.clubId) {
      this.api.getClubPageInfo(this.clubId).subscribe(info => this.slug.set(info.slug));
    }
  }

  onThemeApplied(theme: SiteTheme) {
    this.saveSuccess.set(true);
    setTimeout(() => this.saveSuccess.set(false), 3000);
  }

  saveBranding() {
    this.saving.set(true);
    this.api.updateClubBranding(this.clubId, {
      ...this.branding,
      theme: this.themeService.activeTheme()
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.saveSuccess.set(true);
        setTimeout(() => this.saveSuccess.set(false), 3000);
      },
      error: () => this.saving.set(false)
    });
  }

  copyUrl() {
    navigator.clipboard.writeText(`${window.location.origin}/p/${this.slug()}`);
    this.slugCopied.set(true);
    setTimeout(() => this.slugCopied.set(false), 2500);
  }

  startEditSlug() {
    this.slugEditValue = this.slug();
    this.slugError.set('');
    this.slugSuccess.set(false);
    this.slugConflict.set(false);
    this.slugEditing.set(true);
  }

  cancelEditSlug() {
    this.slugEditing.set(false);
    this.slugError.set('');
  }

  validateSlug() {
    const v = this.slugEditValue;
    if (!v || v.length < 3) {
      this.slugError.set('Minimum 3 characters.');
    } else if (!/^[a-z0-9-]+$/.test(v)) {
      this.slugError.set('Only lowercase letters, numbers, and hyphens.');
    } else {
      this.slugError.set('');
    }
  }

  saveSlug() {
    this.validateSlug();
    if (this.slugError()) return;
    this.slugSaving.set(true);
    this.slugConflict.set(false);
    this.api.updateClubSlug(this.clubId, this.slugEditValue).subscribe({
      next: (newSlug) => {
        this.slug.set(newSlug);
        this.slugSaving.set(false);
        this.slugSuccess.set(true);
        this.slugEditing.set(false);
        setTimeout(() => this.slugSuccess.set(false), 3000);
      },
      error: (err) => {
        this.slugSaving.set(false);
        if (err?.status === 409) {
          this.slugConflict.set(true);
        } else {
          this.slugError.set('Failed to update URL. Please try again.');
        }
      }
    });
  }
}
