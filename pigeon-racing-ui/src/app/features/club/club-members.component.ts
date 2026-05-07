import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe, NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/services';
import { ClubMember, Invitation, InvitationStatus } from '../../core/models';

@Component({
  selector: 'app-club-members',
  standalone: true,
  imports: [DatePipe, NgClass, FormsModule],
  template: `
    <div class="pr-page-header flex justify-between items-center">
      <div>
        <h1 class="pr-page-header__title">Members</h1>
        <p class="pr-page-header__subtitle">{{ members().length }} active members in your club</p>
      </div>
      <button class="pr-btn pr-btn--primary" (click)="activeTab.set('invite')">+ Invite Member</button>
    </div>

    <!-- Tabs -->
    <div class="editor-tabs mb-6">
      <button class="editor-tab" [class.editor-tab--active]="activeTab() === 'members'" (click)="activeTab.set('members')">👥 Members</button>
      <button class="editor-tab" [class.editor-tab--active]="activeTab() === 'invite'"  (click)="activeTab.set('invite')">📧 Invite</button>
      <button class="editor-tab" [class.editor-tab--active]="activeTab() === 'pending'" (click)="activeTab.set('pending')">
        ⏳ Pending
        @if (pendingInvites().length > 0) {
          <span class="badge-count">{{ pendingInvites().length }}</span>
        }
      </button>
    </div>

    <!-- Members list -->
    @if (activeTab() === 'members') {
      <div class="pr-card">
        <div class="flex gap-4 mb-6">
          <input class="pr-input" style="max-width:280px" placeholder="Search members..." [(ngModel)]="search">
        </div>

        @if (filteredMembers().length === 0) {
          <div class="pr-empty">
            <div class="pr-empty__icon">👥</div>
            <div class="pr-empty__title">No members found</div>
          </div>
        } @else {
          <div class="pr-table-wrapper">
            <table class="pr-table">
              <thead>
                <tr><th>Member</th><th>Email</th><th>Role</th><th>Pigeons</th><th>Joined</th><th>Actions</th></tr>
              </thead>
              <tbody>
                @for (m of filteredMembers(); track m.userId) {
                  <tr>
                    <td>
                      <div class="member-cell">
                        <div class="member-avatar">{{ initials(m.fullName) }}</div>
                        <span class="font-bold">{{ m.fullName }}</span>
                      </div>
                    </td>
                    <td class="text-muted text-sm">{{ m.email }}</td>
                    <td><span class="pr-badge pr-badge--info">{{ roleLabel(m.role) }}</span></td>
                    <td>
                      <div class="flex items-center gap-2">
                        <span>{{ m.linkedPigeonCount }}</span>
                        <button class="pr-btn pr-btn--ghost pr-btn--sm" (click)="openLinkPigeon(m)">+ Link</button>
                      </div>
                    </td>
                    <td class="text-muted text-sm">{{ m.joinedAt | date:'dd MMM yyyy' }}</td>
                    <td>
                      <button class="pr-btn pr-btn--danger pr-btn--sm" (click)="removeMember(m.userId)">Remove</button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>

      <!-- Link Pigeon Modal -->
      @if (linkPigeonFor()) {
        <div class="modal-backdrop" (click)="linkPigeonFor.set(null)">
          <div class="modal" (click)="$event.stopPropagation()">
            <h3 style="margin-bottom:16px">Link Pigeon to {{ linkPigeonFor()!.fullName }}</h3>
            <div class="pr-form-group mb-4">
              <label class="pr-label">Ring Number</label>
              <input class="pr-input" [(ngModel)]="newRingNumber" placeholder="BE24-1234567">
            </div>
            <div class="flex gap-3">
              <button class="pr-btn pr-btn--primary" [disabled]="!newRingNumber" (click)="linkPigeon()">Link Pigeon</button>
              <button class="pr-btn pr-btn--ghost" (click)="linkPigeonFor.set(null)">Cancel</button>
            </div>
          </div>
        </div>
      }
    }

    <!-- Invite tab -->
    @if (activeTab() === 'invite') {
      <div class="pr-card" style="max-width:480px">
        <h3 style="margin-bottom:20px">Invite a Fancier</h3>
        <p class="text-muted text-sm mb-4">
          Enter the fancier's email address. They'll receive an invitation link valid for 7 days.
        </p>

        <div class="pr-form-group mb-4">
          <label class="pr-label">Email Address</label>
          <input class="pr-input" type="email" [(ngModel)]="inviteEmail" placeholder="fancier@example.com">
        </div>

        <button class="pr-btn pr-btn--primary"
                [disabled]="!inviteEmail || inviting()"
                (click)="sendInvite()">
          @if (inviting()) { <span class="pr-spinner" style="width:14px;height:14px"></span> }
          📧 Send Invitation
        </button>

        @if (inviteSuccess()) {
          <div class="pr-alert pr-alert--success mt-4">
            ✓ Invitation sent to {{ inviteSuccess() }}
          </div>
        }
        @if (inviteError()) {
          <div class="pr-alert pr-alert--error mt-4">{{ inviteError() }}</div>
        }
      </div>
    }

    <!-- Pending invitations -->
    @if (activeTab() === 'pending') {
      <div class="pr-card">
        <h3 style="margin-bottom:20px">Pending Invitations</h3>
        @if (allInvites().length === 0) {
          <div class="pr-empty">
            <div class="pr-empty__icon">📧</div>
            <div class="pr-empty__title">No invitations sent yet</div>
          </div>
        } @else {
          <div class="pr-table-wrapper">
            <table class="pr-table">
              <thead><tr><th>Email</th><th>Status</th><th>Sent</th><th>Expires</th></tr></thead>
              <tbody>
                @for (inv of allInvites(); track inv.id) {
                  <tr>
                    <td class="font-bold">{{ inv.email }}</td>
                    <td>
                      <span [class]="inviteStatusBadge(inv.status)">{{ InvitationStatus[inv.status] }}</span>
                    </td>
                    <td class="text-muted text-sm">{{ inv.createdAt | date:'dd MMM yyyy HH:mm' }}</td>
                    <td class="text-muted text-sm">{{ inv.expiresAt | date:'dd MMM yyyy' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>
    }
  `,
  styles: [`
    .editor-tabs { display:flex; gap:4px; border-bottom:1px solid var(--pr-border); }
    .editor-tab {
      position:relative; padding:10px 20px; border:none; background:transparent;
      color:var(--pr-text-muted); font-family:var(--font-body); font-size:0.875rem;
      cursor:pointer; border-bottom:2px solid transparent; margin-bottom:-1px;
      transition:all var(--t-fast); display:flex; align-items:center; gap:6px;
    }
    .editor-tab:hover { color:var(--pr-text); }
    .editor-tab--active { color:var(--pr-primary); border-bottom-color:var(--pr-primary); }

    .badge-count {
      background:var(--pr-warning); color:#000;
      border-radius:999px; font-size:0.65rem; font-weight:700;
      padding:1px 6px; min-width:18px; text-align:center;
    }

    .member-cell { display:flex; align-items:center; gap:10px; }
    .member-avatar {
      width:32px; height:32px; border-radius:50%;
      background:var(--pr-primary); color:#fff;
      display:flex; align-items:center; justify-content:center;
      font-family:var(--font-display); font-weight:700; font-size:0.72rem;
      flex-shrink:0;
    }

    .modal-backdrop {
      position:fixed; inset:0; background:rgba(0,0,0,0.6);
      display:flex; align-items:center; justify-content:center; z-index:999;
      backdrop-filter:blur(4px);
    }
    .modal {
      background:var(--pr-surface); border:1px solid var(--pr-border);
      border-radius:calc(var(--pr-radius)*1.5); padding:32px; width:100%; max-width:440px;
      box-shadow:var(--shadow-lg);
    }
  `]
})
export class ClubMembersComponent implements OnInit {
  private api = inject(ApiService);
  private auth = inject(AuthService);
  InvitationStatus = InvitationStatus;

  activeTab   = signal('members');
  members     = signal<ClubMember[]>([]);
  allInvites  = signal<Invitation[]>([]);
  inviting    = signal(false);
  inviteEmail = '';
  inviteSuccess = signal<string | null>(null);
  inviteError   = signal<string | null>(null);
  search = '';
  linkPigeonFor = signal<ClubMember | null>(null);
  newRingNumber = '';
  clubId = '';

  filteredMembers = () => this.members().filter(m =>
    !this.search || m.fullName.toLowerCase().includes(this.search.toLowerCase()) || m.email.includes(this.search)
  );

  pendingInvites = () => this.allInvites().filter(i => i.status === InvitationStatus.Pending);

  ngOnInit() {
    this.clubId = this.auth.clubId() ?? '';
    this.api.getClubMembers(this.clubId).subscribe(p => this.members.set(p.items as ClubMember[]));
    this.api.getClubInvitations(this.clubId).subscribe(i => this.allInvites.set(i));
  }

  sendInvite() {
    this.inviting.set(true);
    this.inviteSuccess.set(null);
    this.inviteError.set(null);
    this.api.inviteMember(this.clubId, this.inviteEmail).subscribe({
      next: inv => {
        this.allInvites.update(arr => [inv, ...arr]);
        this.inviteSuccess.set(this.inviteEmail);
        this.inviteEmail = '';
        this.inviting.set(false);
      },
      error: (e: any) => {
        this.inviteError.set(e?.error?.message ?? 'Failed to send invitation.');
        this.inviting.set(false);
      }
    });
  }

  removeMember(userId: string) {
    this.api.removeMember(this.clubId, userId).subscribe(() => {
      this.members.update(arr => arr.filter(m => m.userId !== userId));
    });
  }

  openLinkPigeon(m: ClubMember) {
    this.linkPigeonFor.set(m);
    this.newRingNumber = '';
  }

  linkPigeon() {
    const m = this.linkPigeonFor();
    if (!m || !this.newRingNumber) return;
    this.api.linkPigeon(m.membershipId, this.newRingNumber).subscribe(() => {
      this.members.update(arr => arr.map(mem =>
        mem.membershipId === m.membershipId
          ? { ...mem, linkedPigeonCount: mem.linkedPigeonCount + 1 }
          : mem
      ));
      this.linkPigeonFor.set(null);
    });
  }

  initials(name: string) { return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2); }

  roleLabel(role: number) {
    return ['', 'Super Admin', 'Country Mgr', 'Club Mgr', 'Fancier'][role] ?? 'Unknown';
  }

  inviteStatusBadge(s: InvitationStatus) {
    const m: Record<number, string> = {
      [InvitationStatus.Pending]:  'pr-badge pr-badge--warning',
      [InvitationStatus.Accepted]: 'pr-badge pr-badge--success',
      [InvitationStatus.Expired]:  'pr-badge pr-badge--muted',
      [InvitationStatus.Revoked]:  'pr-badge pr-badge--error',
    };
    return m[s] ?? 'pr-badge pr-badge--muted';
  }
}
