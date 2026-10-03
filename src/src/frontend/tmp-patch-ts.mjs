const fs = require('fs');
const p = 'src/app/pages/admin/smart-assistant-admin.component.ts';
let d = fs.readFileSync(p, 'utf8');
const changes = [
  ['readonly usersRoleFilter = signal<UserRole | \'\'>(\'\');', 'readonly usersRoleFilter: UserRole | \'\' = \'\';'],
  ['  setUsersRole(role: UserRole | \'\'): void {', '  setUsersRoleString(raw: string): void {\n    const role = (raw === \'Teacher\' || raw === \'Parent\' || raw === \'Student\') ? raw as UserRole : \'\';\n    this.usersRoleFilter = role;\n    this.usersPage.set(1);\n    this.loadUsersPage();\n  }\n\n  setUsersRole(role: UserRole | \'\'): void {'],
  ['  setUsersRole(role: UserRole | \'\'): void {\n    this.usersRoleFilter.set(role);\n    this.usersPage.set(1);\n    this.loadUsersPage();\n  }\n\n  setUsersSort', '  setUsersSort'],
  ['readonly editingClassroom = computed(() => this.classroomEditingId !== null);', 'readonly editingClassroom = signal(false);\n  get editingClassroomSig(): boolean { return this.editingClassroom(); }'],
  ['  // template helpers', '  // template helpers'],
  ['  private clearStatus(): void {', '  formatGradeLabel = formatGradeLabel;\n  locale = this.locale;\n\n  private clearStatus(): void {'],
];

let applied = 0;
changes.forEach(([a, b]) => {
  if (!d.includes(a)) {
    console.log('NOT FOUND:', a.slice(0, 90).replace(/\n/g, '\\n'));
  } else {
    d = d.split(a).join(b);
    applied++;
  }
});

fs.writeFileSync(p, d);
console.log('PATCHED bytes', d.length, 'changes', applied);
