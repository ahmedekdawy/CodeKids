import { PanelNavItem } from './layouts/panel-shell/panel-shell.component';

/** What kind of organisation the tenant is; chosen when the tenant registers. */
export type TenantStyle = 'School' | 'Academy' | 'SingleTeacher';

export const TENANT_STYLES: TenantStyle[] = ['School', 'Academy', 'SingleTeacher'];

export function normalizeTenantStyle(raw: string | null | undefined): TenantStyle {
  return TENANT_STYLES.find((style) => style.toLowerCase() === (raw ?? '').trim().toLowerCase()) ?? 'School';
}

/** School-curriculum pages: the fixed weekly timetable and grade certificates. */
const SCHOOL_ONLY_PATHS = [
  '/admin/timetable',
  '/admin/grade-certificates',
  '/teacher/timetable',
  '/teacher/grade-certificates'
];

/** Pages for managing a staff of several admins and teachers. */
const STAFF_PATHS = ['/admin/admins', '/admin/payroll', '/admin/attendance', '/teacher/attendance'];

/** Menu paths hidden for each style. A school sees everything. */
const HIDDEN_PATHS: Record<TenantStyle, string[]> = {
  School: [],
  Academy: SCHOOL_ONLY_PATHS,
  SingleTeacher: [...SCHOOL_ONLY_PATHS, ...STAFF_PATHS]
};

export function navItemsForStyle(items: PanelNavItem[], style: TenantStyle): PanelNavItem[] {
  const hidden = HIDDEN_PATHS[style];
  return hidden.length ? items.filter((item) => !hidden.includes(item.path)) : items;
}
