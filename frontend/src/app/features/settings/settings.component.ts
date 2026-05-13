import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService, ThemeService } from '../../core/services/services';
import { ApiService } from '../../core/services/api.service';
import { SiteTheme, UserRole } from '../../core/models';
import { TranslationService, SUPPORTED_LOCALES, TranslatePipe } from '../../core/i18n';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [FormsModule, NgClass, RouterLink, TranslatePipe],
  template: `
    <div class="settings-page">
      <div class="settings-header">
        <h1 class="settings-title">{{ 'settings.title' | translate }}</h1>
        <p class="settings-subtitle">{{ 'settings.subtitle' | translate }}</p>
      </div>

      <!-- Tabs -->
      <div class="settings-tabs">
        <button class="settings-tab" [class.settings-tab--active]="tab() === 'profile'"       (click)="tab.set('profile')">{{ 'settings.tabProfile' | translate }}</button>
        <button class="settings-tab" [class.settings-tab--active]="tab() === 'account'"       (click)="tab.set('account')">{{ 'settings.tabAccount' | translate }}</button>
        <button class="settings-tab" [class.settings-tab--active]="tab() === 'security'"      (click)="tab.set('security')">{{ 'settings.tabSecurity' | translate }}</button>
        <button class="settings-tab" [class.settings-tab--active]="tab() === 'notifications'" (click)="tab.set('notifications')">{{ 'settings.tabNotifications' | translate }}</button>
        <button class="settings-tab" [class.settings-tab--active]="tab() === 'appearance'"    (click)="tab.set('appearance')">{{ 'settings.tabAppearance' | translate }}</button>
      </div>

      <!-- ── Profile Tab ───────────────────────────────────────── -->
      @if (tab() === 'profile') {
        <div class="settings-card">
          <div class="settings-card__header">
            <div class="settings-avatar">{{ initials() }}</div>
            <div>
              <div class="settings-card__title">{{ 'settings.personalInfo' | translate }}</div>
              <div class="settings-card__sub">{{ roleName() }}</div>
            </div>
          </div>

          <div class="settings-form">
            <div class="form-row">
              <div class="form-group">
                <label class="pr-label">{{ 'auth.firstName' | translate }}</label>
                <input class="pr-input" [(ngModel)]="firstName">
              </div>
              <div class="form-group">
                <label class="pr-label">{{ 'auth.lastName' | translate }}</label>
                <input class="pr-input" [(ngModel)]="lastName">
              </div>
            </div>
            <div class="form-group">
              <label class="pr-label">{{ 'auth.email' | translate }}</label>
              <input class="pr-input" [value]="auth.currentUser()?.email ?? ''" disabled>
              <span class="form-hint">{{ 'settings.emailCannotChange' | translate }}</span>
            </div>

            @if (profileMsg()) {
              <div class="settings-msg" [ngClass]="profileMsgType() === 'ok' ? 'settings-msg--ok' : 'settings-msg--err'">
                {{ profileMsg() }}
              </div>
            }

            <div class="form-actions">
              <button class="pr-btn pr-btn--primary" (click)="saveProfile()" [disabled]="profileSaving()">
                {{ (profileSaving() ? 'settings.saving' : 'settings.saveChanges') | translate }}
              </button>
            </div>
          </div>
        </div>
      }

      <!-- ── Account Tab ───────────────────────────────────────── -->
      @if (tab() === 'account') {

        <!-- Role + info card -->
        <div class="settings-card">
          <div class="settings-card__header">
            <div class="role-icon">{{ roleIcon() }}</div>
            <div>
              <div class="settings-card__title">{{ roleName() }}</div>
              <div class="settings-card__sub">Current role</div>
            </div>
          </div>

          <div class="account-info-grid">
            <div class="account-info-item">
              <span class="account-info-item__label">Email</span>
              <span class="account-info-item__val">{{ auth.currentUser()?.email }}</span>
            </div>
            <div class="account-info-item">
              <span class="account-info-item__label">Full Name</span>
              <span class="account-info-item__val">{{ auth.currentUser()?.fullName }}</span>
            </div>
            <div class="account-info-item">
              <span class="account-info-item__label">Status</span>
              <span class="pr-badge pr-badge--success">Active</span>
            </div>

            @if (auth.currentUser()?.role === UserRole.ClubManager || auth.currentUser()?.role === UserRole.Fancier) {
              <div class="account-info-item">
                <span class="account-info-item__label">Club</span>
                <span class="account-info-item__val">{{ clubName() ?? '—' }}</span>
              </div>
            }

            @if (auth.currentUser()?.role === UserRole.FederationManager || auth.currentUser()?.role === UserRole.ClubManager) {
              <div class="account-info-item">
                <span class="account-info-item__label">Country</span>
                <span class="account-info-item__val">{{ federationName() ?? '—' }}</span>
              </div>
            }

            @if (auth.currentUser()?.role === UserRole.SuperAdmin) {
              <div class="account-info-item">
                <span class="account-info-item__label">Scope</span>
                <span class="account-info-item__val">Full platform access</span>
              </div>
            }
          </div>
        </div>

        <!-- Pending upgrade requests (any role that has them) -->
        @if (pendingRequests().length > 0) {
          <div class="settings-card settings-card--highlight" style="margin-top:16px">
            <div class="settings-card__header" style="padding-bottom:14px;margin-bottom:14px">
              <div class="role-icon">⬆️</div>
              <div>
                <div class="settings-card__title">Pending Role Requests</div>
                <div class="settings-card__sub">Awaiting review by admin</div>
              </div>
            </div>

            @for (r of pendingRequests(); track r.id) {
              <div class="upgrade-request-item upgrade-request-item--pending">
                <div class="upgrade-request-item__info">
                  <span class="upgrade-request-item__role">{{ upgradeRoleName(r.requestedRole) }}</span>
                  @if (r.federationName) {
                    <span class="text-muted text-sm">· {{ r.federationName }}</span>
                  }
                  @if (r.clubName) {
                    <span class="text-muted text-sm">· {{ r.clubName }}</span>
                  }
                  <span class="text-muted text-sm">· Submitted {{ formatDate(r.createdAt) }}</span>
                </div>
                <button class="pr-btn pr-btn--ghost pr-btn--sm"
                        [disabled]="reminderCooldown(r.id) > 0 || reminderSending() === r.id"
                        (click)="sendReminder(r.id)">
                  @if (reminderSending() === r.id) {
                    <span class="pr-spinner" style="width:12px;height:12px"></span>
                  } @else if (reminderCooldown(r.id) > 0) {
                    Remind again in {{ cooldownHours(r.id) }}
                  } @else {
                    Request Update
                  }
                </button>
              </div>
            }
          </div>
        }

        <!-- Role-specific panels (quick links + upgrade CTA for fancier) -->
        @if (auth.currentUser()?.role === UserRole.Fancier) {
          <div class="settings-card" style="margin-top:16px">
            <div class="settings-card__header" style="border-bottom:none;margin-bottom:8px;padding-bottom:0">
              <div class="role-icon">⬆️</div>
              <div>
                <div class="settings-card__title">Want more access?</div>
                <div class="settings-card__sub">Role upgrade</div>
              </div>
            </div>
            <p class="text-muted" style="font-size:0.875rem;line-height:1.6;margin-bottom:16px">
              As a Fancier you can view races and results. Submit a role upgrade request to become a
              <strong>Club Manager</strong> or <strong>Federation Manager</strong>.
            </p>
            <a class="pr-btn pr-btn--primary" routerLink="/auth/upgrade-request">Request Role Upgrade</a>
          </div>
        }

        @if (auth.currentUser()?.role === UserRole.ClubManager) {
          <div class="settings-card" style="margin-top:16px">
            <div class="settings-card__header" style="border-bottom:none;margin-bottom:0;padding-bottom:0">
              <div class="role-icon">🏟️</div>
              <div>
                <div class="settings-card__title">Club Manager</div>
                <div class="settings-card__sub">Quick links</div>
              </div>
            </div>
            <div class="quick-links">
              <a class="quick-link" routerLink="/club/dashboard">📊 Dashboard</a>
              <a class="quick-link" routerLink="/club/races">🏁 Races</a>
              <a class="quick-link" routerLink="/club/members">👥 Members</a>
              <a class="quick-link" routerLink="/club/page">🎨 Club Page</a>
              <a class="quick-link" routerLink="/auth/upgrade-request">⬆️ Request FM Role</a>
            </div>
          </div>
        }

        @if (auth.currentUser()?.role === UserRole.FederationManager) {
          <div class="settings-card" style="margin-top:16px">
            <div class="settings-card__header" style="border-bottom:none;margin-bottom:0;padding-bottom:0">
              <div class="role-icon">🌍</div>
              <div>
                <div class="settings-card__title">Federation Manager</div>
                <div class="settings-card__sub">Quick links</div>
              </div>
            </div>
            <div class="quick-links">
              <a class="quick-link" routerLink="/federation/dashboard">📊 Dashboard</a>
              <a class="quick-link" routerLink="/federation/clubs">🏟️ Clubs</a>
              <a class="quick-link" routerLink="/federation/members">👥 Members</a>
              <a class="quick-link" routerLink="/federation/page">🎨 Federation Page</a>
              <a class="quick-link" routerLink="/federation/upgrade-requests">⬆️ Manage Role Requests</a>
            </div>
          </div>
        }

        @if (auth.currentUser()?.role === UserRole.SuperAdmin) {
          <div class="settings-card" style="margin-top:16px">
            <div class="settings-card__header" style="border-bottom:none;margin-bottom:0;padding-bottom:0">
              <div class="role-icon">🛡️</div>
              <div>
                <div class="settings-card__title">Super Admin</div>
                <div class="settings-card__sub">Quick links</div>
              </div>
            </div>
            <div class="quick-links">
              <a class="quick-link" routerLink="/admin/dashboard">📊 Dashboard</a>
              <a class="quick-link" routerLink="/admin/users">👥 Users</a>
              <a class="quick-link" routerLink="/admin/federations">🌍 Federations</a>
              <a class="quick-link" routerLink="/admin/upgrade-requests">⬆️ Upgrade Requests</a>
            </div>
          </div>
        }

        <!-- All requests history (collapsed view) -->
        @if (upgradeRequests().length > 0) {
          <div class="settings-card" style="margin-top:16px">
            <div class="settings-card__header" style="border-bottom:none;margin-bottom:8px;padding-bottom:0">
              <div class="settings-card__title">Request History</div>
            </div>
            @for (r of upgradeRequests(); track r.id) {
              <div class="upgrade-request-item">
                <div class="upgrade-request-item__info" style="flex:1">
                  <span class="upgrade-request-item__role">{{ upgradeRoleName(r.requestedRole) }}</span>
                  @if (r.federationName) { <span class="text-muted text-sm">· {{ r.federationName }}</span> }
                  @if (r.clubName)    { <span class="text-muted text-sm">· {{ r.clubName }}</span> }
                  <span class="text-muted text-sm">· {{ formatDate(r.createdAt) }}</span>
                </div>
                <span [class]="'pr-badge ' + upgradeStatusBadge(r.status)">{{ upgradeStatusLabel(r.status) }}</span>
              </div>
            }
          </div>
        }
      }

      <!-- ── Security Tab ──────────────────────────────────────── -->
      @if (tab() === 'security') {
        <div class="settings-card">
          <div class="settings-card__header">
            <div class="settings-card__title">{{ 'settings.changePassword' | translate }}</div>
          </div>

          <div class="settings-form">
            <div class="form-group">
              <label class="pr-label">{{ 'settings.currentPassword' | translate }}</label>
              <input class="pr-input" type="password" [(ngModel)]="currentPassword">
            </div>
            <div class="form-group">
              <label class="pr-label">{{ 'settings.newPassword' | translate }}</label>
              <input class="pr-input" type="password" [(ngModel)]="newPassword">
            </div>
            <div class="form-group">
              <label class="pr-label">{{ 'settings.confirmNewPassword' | translate }}</label>
              <input class="pr-input" type="password" [(ngModel)]="confirmPassword">
            </div>

            @if (pwMsg()) {
              <div class="settings-msg" [ngClass]="pwMsgType() === 'ok' ? 'settings-msg--ok' : 'settings-msg--err'">
                {{ pwMsg() }}
              </div>
            }

            <div class="form-actions">
              <button class="pr-btn pr-btn--primary" (click)="changePassword()" [disabled]="pwSaving()">
                {{ (pwSaving() ? 'settings.saving' : 'settings.updatePassword') | translate }}
              </button>
            </div>
          </div>
        </div>

        <div class="settings-card" style="margin-top:16px">
          <div class="settings-card__header">
            <div class="settings-card__title">{{ 'auth.logout' | translate }}</div>
          </div>
          <button class="pr-btn pr-btn--danger" (click)="auth.logout()">{{ 'settings.signOut' | translate }}</button>
        </div>
      }

      <!-- ── Notifications Tab ────────────────────────────────── -->
      @if (tab() === 'notifications') {
        <div class="settings-card">
          <div class="settings-card__header">
            <div class="role-icon">🔔</div>
            <div>
              <div class="settings-card__title">Email Notifications</div>
              <div class="settings-card__sub">Choose which emails you receive</div>
            </div>
          </div>
          <div class="notif-prefs">
            @for (pref of notifPrefs; track pref.key) {
              <div class="notif-pref-row">
                <div class="notif-pref-info">
                  <div class="notif-pref-info__title">{{ pref.label }}</div>
                  <div class="notif-pref-info__desc">{{ pref.desc }}</div>
                </div>
                <label class="toggle">
                  <input type="checkbox" [(ngModel)]="pref.enabled">
                  <span class="toggle__track"><span class="toggle__thumb"></span></span>
                </label>
              </div>
            }
          </div>
          <div class="form-actions" style="margin-top:20px">
            <button class="pr-btn pr-btn--primary" (click)="saveNotifPrefs()">Save Preferences</button>
          </div>
          @if (notifMsg()) {
            <div class="settings-msg settings-msg--ok" style="margin-top:12px">{{ notifMsg() }}</div>
          }
        </div>

        <div class="settings-card" style="margin-top:16px">
          <div class="settings-card__header" style="border-bottom:none;margin-bottom:0;padding-bottom:0">
            <div class="role-icon">📥</div>
            <div>
              <div class="settings-card__title">In-App Notifications</div>
              <div class="settings-card__sub">Always on</div>
            </div>
          </div>
          <p class="text-muted" style="font-size:0.875rem;line-height:1.6;margin-top:12px">
            In-app notifications are always enabled. Visit the
            <a routerLink="/{{ notifPath() }}" style="color:var(--pr-primary)">Notifications page</a>
            to view and manage your inbox.
          </p>
        </div>
      }

      <!-- ── Appearance Tab ────────────────────────────────────── -->
      @if (tab() === 'appearance') {
        <div class="settings-card">
          <div class="settings-card__header">
            <div class="settings-card__title">Theme</div>
          </div>
          <div class="theme-grid">
            @for (t of themes; track t.id) {
              <button
                class="theme-card"
                [class.theme-card--active]="themeService.activeTheme() === t.id"
                (click)="themeService.applyTheme(t.id)">
                <div class="theme-card__swatch" [style.background]="t.gradient"></div>
                <div class="theme-card__name">{{ t.name }}</div>
                @if (themeService.activeTheme() === t.id) {
                  <div class="theme-card__check">✓</div>
                }
              </button>
            }
          </div>
        </div>

        <div class="settings-card" style="margin-top:16px">
          <div class="settings-card__header" style="border-bottom:none;margin-bottom:16px;padding-bottom:0">
            <div class="settings-card__title">Language</div>
          </div>
          <div class="lang-grid">
            @for (loc of locales; track loc.code) {
              <button class="lang-btn" [class.lang-btn--active]="i18n.locale() === loc.code"
                      (click)="setLocale(loc.code)">
                <span class="lang-btn__flag">{{ loc.flag }}</span>
                <span class="lang-btn__name">{{ loc.name }}</span>
                @if (i18n.locale() === loc.code) { <span class="lang-btn__check">✓</span> }
              </button>
            }
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .settings-page { max-width: 720px; }

    .settings-header { margin-bottom: 28px; }
    .settings-title { font-size: 1.6rem; font-weight: 800; color: var(--pr-text); margin: 0 0 4px; }
    .settings-subtitle { color: var(--pr-text-muted); font-size: 0.9rem; }

    /* Tabs */
    .settings-tabs {
      display: flex; gap: 4px; margin-bottom: 24px;
      border-bottom: 1px solid var(--pr-border);
    }
    .settings-tab {
      padding: 10px 20px; border: none; background: none; cursor: pointer;
      color: var(--pr-text-muted); font-weight: 500; font-size: 0.875rem;
      border-bottom: 2px solid transparent; margin-bottom: -1px;
      transition: color var(--t-fast), border-color var(--t-fast);
    }
    .settings-tab:hover { color: var(--pr-text); }
    .settings-tab--active { color: var(--pr-primary); border-bottom-color: var(--pr-primary); }

    /* Card */
    .settings-card {
      background: var(--pr-surface);
      border: 1px solid var(--pr-border);
      border-radius: var(--pr-radius);
      padding: 24px;
    }
    .settings-card--highlight {
      border-color: var(--pr-primary);
      background: rgba(30,144,255,0.04);
    }
    .settings-card__header {
      display: flex; align-items: center; gap: 16px;
      margin-bottom: 24px; padding-bottom: 20px;
      border-bottom: 1px solid var(--pr-border);
    }
    .settings-card__title { font-weight: 700; font-size: 1rem; color: var(--pr-text); }
    .settings-card__sub { font-size: 0.78rem; color: var(--pr-text-muted); margin-top: 2px; text-transform: uppercase; letter-spacing: 0.06em; }

    /* Avatar + Role icon */
    .settings-avatar, .role-icon {
      width: 52px; height: 52px; border-radius: 50%;
      background: var(--pr-primary); color: #fff;
      display: flex; align-items: center; justify-content: center;
      font-weight: 800; font-size: 1.1rem; flex-shrink: 0;
    }
    .role-icon { background: var(--pr-surface-2); font-size: 1.4rem; }

    /* Form */
    .settings-form { display: flex; flex-direction: column; gap: 16px; }
    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
    .form-group { display: flex; flex-direction: column; gap: 6px; }
    .pr-label { font-size: 0.8rem; font-weight: 600; color: var(--pr-text-muted); text-transform: uppercase; letter-spacing: 0.05em; }
    .form-hint { font-size: 0.75rem; color: var(--pr-text-muted); }
    .form-actions { display: flex; justify-content: flex-end; margin-top: 8px; }

    /* Account info grid */
    .account-info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }
    .account-info-item { display: flex; flex-direction: column; gap: 4px; }
    .account-info-item__label { font-size: 0.72rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.06em; color: var(--pr-text-muted); }
    .account-info-item__val { font-size: 0.9rem; color: var(--pr-text); }
    .mono { font-family: monospace; font-size: 0.8rem; background: var(--pr-surface-2); padding: 2px 6px; border-radius: 4px; word-break: break-all; }

    /* Upgrade requests */
    .upgrade-requests { margin-top: 20px; padding-top: 16px; border-top: 1px solid var(--pr-border); }
    .upgrade-requests__label { font-size: 0.75rem; font-weight: 700; text-transform: uppercase; letter-spacing: 0.06em; color: var(--pr-text-muted); margin-bottom: 10px; }
    .upgrade-request-item { display: flex; align-items: center; gap: 10px; padding: 10px 0; border-bottom: 1px solid var(--pr-border); flex-wrap: wrap; }
    .upgrade-request-item:last-child { border-bottom: none; }
    .upgrade-request-item__role { font-weight: 600; font-size: 0.875rem; }
    .upgrade-request-item--pending { padding: 12px 0; }
    .upgrade-request-item__info { display: flex; align-items: center; gap: 8px; flex: 1; flex-wrap: wrap; }

    /* Quick links */
    .quick-links { display: flex; flex-wrap: wrap; gap: 10px; margin-top: 16px; }
    .quick-link {
      display: inline-flex; align-items: center; gap: 6px;
      padding: 8px 14px; border-radius: var(--pr-radius);
      background: var(--pr-surface-2); border: 1px solid var(--pr-border);
      color: var(--pr-text); font-size: 0.875rem; font-weight: 500;
      transition: background var(--t-fast), border-color var(--t-fast);
    }
    .quick-link:hover { background: var(--pr-surface); border-color: var(--pr-primary); color: var(--pr-primary); }

    /* Messages */
    .settings-msg {
      padding: 10px 14px; border-radius: var(--pr-radius);
      font-size: 0.875rem; font-weight: 500;
    }
    .settings-msg--ok  { background: rgba(0,230,118,0.1); color: var(--pr-success); border: 1px solid var(--pr-success); }
    .settings-msg--err { background: rgba(255,82,82,0.1);  color: var(--pr-error);   border: 1px solid var(--pr-error); }

    /* Theme grid */
    .theme-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(120px, 1fr)); gap: 12px; }
    .theme-card {
      border: 2px solid var(--pr-border); border-radius: var(--pr-radius);
      padding: 0; overflow: hidden; cursor: pointer; background: var(--pr-surface-2);
      transition: border-color var(--t-fast), transform var(--t-fast);
      position: relative;
    }
    .theme-card:hover { transform: translateY(-2px); border-color: var(--pr-text-muted); }
    .theme-card--active { border-color: var(--pr-primary); }
    .theme-card__swatch { height: 56px; }
    .theme-card__name { padding: 8px 10px; font-size: 0.8rem; font-weight: 600; color: var(--pr-text); }
    .theme-card__check {
      position: absolute; top: 6px; right: 6px;
      width: 20px; height: 20px; border-radius: 50%;
      background: var(--pr-primary); color: #fff;
      display: flex; align-items: center; justify-content: center;
      font-size: 0.7rem; font-weight: 800;
    }

    /* Language buttons */
    .lang-grid { display: flex; flex-wrap: wrap; gap: 10px; }
    .lang-btn {
      display: flex; align-items: center; gap: 10px;
      padding: 10px 16px; border: 2px solid var(--pr-border);
      border-radius: var(--pr-radius); cursor: pointer; background: var(--pr-surface-2);
      font-size: 0.875rem; font-weight: 500; color: var(--pr-text);
      transition: border-color var(--t-fast), background var(--t-fast);
    }
    .lang-btn:hover { border-color: var(--pr-primary); }
    .lang-btn--active { border-color: var(--pr-primary); background: rgba(30,144,255,0.08); }
    .lang-btn__flag { font-size: 1.3rem; line-height: 1; }
    .lang-btn__name { white-space: nowrap; }
    .lang-btn__check { color: var(--pr-primary); font-weight: 800; margin-left: 4px; }

    /* Notification preferences */
    .notif-prefs { display: flex; flex-direction: column; gap: 0; }
    .notif-pref-row {
      display: flex; align-items: center; justify-content: space-between;
      padding: 14px 0; border-bottom: 1px solid var(--pr-border);
    }
    .notif-pref-row:last-child { border-bottom: none; }
    .notif-pref-info__title { font-size: 0.9rem; font-weight: 600; color: var(--pr-text); margin-bottom: 2px; }
    .notif-pref-info__desc  { font-size: 0.78rem; color: var(--pr-text-muted); }

    /* Toggle switch */
    .toggle { position: relative; display: inline-flex; cursor: pointer; }
    .toggle input { position: absolute; opacity: 0; width: 0; height: 0; }
    .toggle__track {
      width: 42px; height: 24px; border-radius: 12px;
      background: var(--pr-border); transition: background var(--t-fast);
      display: flex; align-items: center; padding: 2px;
    }
    .toggle input:checked + .toggle__track { background: var(--pr-primary); }
    .toggle__thumb {
      width: 20px; height: 20px; border-radius: 50%; background: #fff;
      transition: transform var(--t-fast); flex-shrink: 0;
    }
    .toggle input:checked + .toggle__track .toggle__thumb { transform: translateX(18px); }

    @media (max-width: 600px) {
      .form-row, .account-info-grid { grid-template-columns: 1fr; }
    }
  `]
})
export class SettingsComponent implements OnInit {
  auth         = inject(AuthService);
  themeService = inject(ThemeService);
  i18n         = inject(TranslationService);
  private api  = inject(ApiService);

  locales = SUPPORTED_LOCALES;
  setLocale(code: string) { this.i18n.setLocale(code); }

  UserRole = UserRole;

  tab = signal<'profile' | 'account' | 'security' | 'notifications' | 'appearance'>('profile');

  // Profile form
  firstName = this.auth.currentUser()?.firstName ?? '';
  lastName  = this.auth.currentUser()?.lastName  ?? '';
  profileSaving  = signal(false);
  profileMsg     = signal('');
  profileMsgType = signal<'ok' | 'err'>('ok');

  // Password form
  currentPassword = '';
  newPassword     = '';
  confirmPassword = '';
  pwSaving  = signal(false);
  pwMsg     = signal('');
  pwMsgType = signal<'ok' | 'err'>('ok');

  // Upgrade requests (all roles) + name lookups
  upgradeRequests = signal<any[]>([]);
  clubName        = signal<string | null>(null);
  federationName     = signal<string | null>(null);
  reminderSending = signal<string | null>(null);

  pendingRequests = computed(() =>
    this.upgradeRequests().filter(r => r.status === 0)
  );

  // Notification preferences (localStorage-backed)
  notifMsg  = signal('');
  notifPrefs = this.loadNotifPrefs();

  private loadNotifPrefs() {
    const userId = this.auth.currentUser()?.id ?? 'anon';
    const raw = localStorage.getItem(`pr_notif_prefs_${userId}`);
    const saved: Record<string, boolean> = raw ? JSON.parse(raw) : {};
    return [
      { key: 'raceResult',      label: 'Race Results Published', desc: 'Email when race results you participated in are published', enabled: saved['raceResult']      ?? true  },
      { key: 'raceAnnounce',    label: 'Race Announcements',     desc: 'Email about upcoming races in your club or federation',     enabled: saved['raceAnnounce']    ?? true  },
      { key: 'clubUpdate',      label: 'Club Updates',           desc: 'Email about changes to your club or membership status',     enabled: saved['clubUpdate']      ?? true  },
      { key: 'approval',        label: 'Approvals & Rejections', desc: 'Email when your requests are reviewed by an admin',         enabled: saved['approval']        ?? true  },
      { key: 'invitation',      label: 'Invitations',            desc: 'Email when you are invited to join a club',                 enabled: saved['invitation']      ?? true  },
      { key: 'systemUpdate',    label: 'System Notifications',   desc: 'Platform announcements and maintenance notices',            enabled: saved['systemUpdate']    ?? false },
    ];
  }

  saveNotifPrefs() {
    const userId = this.auth.currentUser()?.id ?? 'anon';
    const record: Record<string, boolean> = {};
    this.notifPrefs.forEach(p => record[p.key] = p.enabled);
    localStorage.setItem(`pr_notif_prefs_${userId}`, JSON.stringify(record));
    this.notifMsg.set('Preferences saved.');
    setTimeout(() => this.notifMsg.set(''), 3000);
  }

  notifPath = computed(() => {
    const paths: Record<number, string> = {
      1: 'admin/notifications', 2: 'federation/notifications',
      3: 'club/notifications',  4: 'fancier/notifications',
    };
    return paths[this.auth.currentUser()?.role ?? 0] ?? 'fancier/notifications';
  });

  ngOnInit() {
    if (this.auth.currentUser()?.role === UserRole.SuperAdmin) return;
    this.api.getMyUpgradeRequests().subscribe({
      next: requests => {
        // Enrich each request with country name from the federation lookup
        this.upgradeRequests.set(requests);
        if (requests.some((r: any) => r.federationId)) {
          this.api.getPublicFederations().subscribe({
            next: feds => {
              this.upgradeRequests.update(reqs => reqs.map((r: any) => ({
                ...r,
                federationName: feds.find(f => f.id === r.federationId)?.name ?? null
              })));
            },
            error: () => {}
          });
        }
      },
      error: () => {}
    });

    // Load club name
    const clubId = this.auth.clubId();
    if (clubId) {
      this.api.getClub(clubId).subscribe({
        next: c => this.clubName.set(c.name),
        error: () => {}
      });
    }

    // Load country/federation name
    const fedId = this.auth.FederationId();
    if (fedId) {
      this.api.getPublicFederations().subscribe({
        next: feds => this.federationName.set(feds.find(f => f.id === fedId)?.name ?? null),
        error: () => {}
      });
    }
  }

  initials = computed(() => {
    const u = this.auth.currentUser();
    if (!u) return '?';
    return `${u.firstName?.[0] ?? ''}${u.lastName?.[0] ?? ''}`.toUpperCase();
  });

  roleName = computed(() => {
    const roles: Record<number, string> = {
      1: 'Super Admin', 2: 'Federation Manager', 3: 'Club Manager', 4: 'Fancier'
    };
    return roles[this.auth.currentUser()?.role ?? 0] ?? 'Unknown';
  });

  roleIcon = computed(() => {
    const icons: Record<number, string> = {
      1: '🛡️', 2: '🌍', 3: '🏟️', 4: '🕊️'
    };
    return icons[this.auth.currentUser()?.role ?? 0] ?? '👤';
  });

  themes = [
    { id: SiteTheme.Skyline, name: 'Skyline', gradient: 'linear-gradient(135deg, #0A1520 50%, #1E90FF)' },
    { id: SiteTheme.Meadow,  name: 'Meadow',  gradient: 'linear-gradient(135deg, #F9F3E8 50%, #2D6A4F)' },
    { id: SiteTheme.Crimson, name: 'Crimson', gradient: 'linear-gradient(135deg, #F5F5F5 50%, #C1121F)' },
    { id: SiteTheme.Ivory,   name: 'Ivory',   gradient: 'linear-gradient(135deg, #FAF7F0 50%, #B8860B)' },
    { id: SiteTheme.Slate,   name: 'Slate',   gradient: 'linear-gradient(135deg, #F7FAFC 50%, #4A5568)' },
  ];

  saveProfile() {
    if (!this.firstName.trim() || !this.lastName.trim()) {
      this.profileMsg.set('First and last name are required.');
      this.profileMsgType.set('err');
      return;
    }
    this.profileSaving.set(true);
    this.profileMsg.set('');
    this.api.updateProfile(this.firstName.trim(), this.lastName.trim()).subscribe({
      next: (user) => {
        this.auth.setTokens(
          this.auth.getAccessToken()!,
          this.auth.getRefreshToken()!,
          user
        );
        this.profileMsg.set('Profile updated successfully.');
        this.profileMsgType.set('ok');
        this.profileSaving.set(false);
      },
      error: (err) => {
        this.profileMsg.set(err?.error?.message ?? 'Failed to update profile.');
        this.profileMsgType.set('err');
        this.profileSaving.set(false);
      }
    });
  }

  changePassword() {
    if (!this.currentPassword || !this.newPassword || !this.confirmPassword) {
      this.pwMsg.set('All fields are required.');
      this.pwMsgType.set('err');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.pwMsg.set('New passwords do not match.');
      this.pwMsgType.set('err');
      return;
    }
    if (this.newPassword.length < 8) {
      this.pwMsg.set('New password must be at least 8 characters.');
      this.pwMsgType.set('err');
      return;
    }
    this.pwSaving.set(true);
    this.pwMsg.set('');
    const userId = this.auth.currentUser()!.id;
    this.api.changePassword(userId, this.currentPassword, this.newPassword).subscribe({
      next: () => {
        this.pwMsg.set('Password changed successfully.');
        this.pwMsgType.set('ok');
        this.pwSaving.set(false);
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
      },
      error: (err) => {
        this.pwMsg.set(err?.error?.message ?? 'Failed to change password.');
        this.pwMsgType.set('err');
        this.pwSaving.set(false);
      }
    });
  }

  upgradeRoleName(role: number): string {
    return { 2: 'Federation Manager', 3: 'Club Manager' }[role] ?? `Role ${role}`;
  }

  upgradeStatusLabel(status: number): string {
    return { 0: 'Pending', 1: 'Approved', 2: 'Rejected' }[status] ?? 'Unknown';
  }

  upgradeStatusBadge(status: number): string {
    return { 0: 'pr-badge--warning', 1: 'pr-badge--success', 2: 'pr-badge--error' }[status] ?? 'pr-badge--muted';
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  private readonly COOLDOWN_MS = 24 * 60 * 60 * 1000;

  reminderCooldown(requestId: string): number {
    const stored = localStorage.getItem(`upg_reminder_${requestId}`);
    if (!stored) return 0;
    return Math.max(0, this.COOLDOWN_MS - (Date.now() - parseInt(stored, 10)));
  }

  cooldownHours(requestId: string): string {
    const ms = this.reminderCooldown(requestId);
    const h  = Math.ceil(ms / 3600000);
    return h === 1 ? '1 hour' : `${h} hours`;
  }

  sendReminder(requestId: string) {
    this.reminderSending.set(requestId);
    this.api.sendUpgradeReminder(requestId).subscribe({
      next: () => {
        localStorage.setItem(`upg_reminder_${requestId}`, String(Date.now()));
        this.reminderSending.set(null);
      },
      error: () => this.reminderSending.set(null)
    });
  }
}
