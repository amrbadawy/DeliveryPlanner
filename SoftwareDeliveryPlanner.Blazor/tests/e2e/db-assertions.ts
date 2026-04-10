import path from 'node:path';
import Database from 'better-sqlite3';

const dbPath = process.env.PLANNER_DB_PATH ?? path.join(process.cwd(), '.playwright', 'planner-e2e.db');

function withDb<T>(fn: (db: Database.Database) => T): T {
  const db = new Database(dbPath, { fileMustExist: true });
  try {
    return fn(db);
  } finally {
    db.close();
  }
}

function tableExists(db: Database.Database, tableName: string): boolean {
  const row = db
    .prepare("SELECT COUNT(1) AS cnt FROM sqlite_master WHERE type = 'table' AND name = ?")
    .get(tableName) as { cnt: number };
  return (row?.cnt ?? 0) > 0;
}

export function countTasksByTaskId(taskId: string): number {
  return withDb((db) => {
    if (!tableExists(db, 'Tasks')) return 0;
    const row = db.prepare('SELECT COUNT(1) AS cnt FROM Tasks WHERE TaskId = ?').get(taskId) as { cnt: number };
    return row?.cnt ?? 0;
  });
}

export function getTaskByTaskId(taskId: string): { taskId: string; serviceName: string; devEstimation: number; priority: number } | null {
  return withDb((db) => {
    if (!tableExists(db, 'Tasks')) return null;
    const row = db
      .prepare('SELECT TaskId AS taskId, ServiceName AS serviceName, DevEstimation AS devEstimation, Priority AS priority FROM Tasks WHERE TaskId = ?')
      .get(taskId) as { taskId: string; serviceName: string; devEstimation: number; priority: number } | undefined;
    return row ?? null;
  });
}

export function countResourcesByResourceId(resourceId: string): number {
  return withDb((db) => {
    if (!tableExists(db, 'Resources')) return 0;
    const row = db.prepare('SELECT COUNT(1) AS cnt FROM Resources WHERE ResourceId = ?').get(resourceId) as { cnt: number };
    return row?.cnt ?? 0;
  });
}

export function getResourceByResourceId(resourceId: string): { resourceId: string; resourceName: string; team: string; availabilityPct: number } | null {
  return withDb((db) => {
    if (!tableExists(db, 'Resources')) return null;
    const row = db
      .prepare('SELECT ResourceId AS resourceId, ResourceName AS resourceName, Team AS team, AvailabilityPct AS availabilityPct FROM Resources WHERE ResourceId = ?')
      .get(resourceId) as { resourceId: string; resourceName: string; team: string; availabilityPct: number } | undefined;
    return row ?? null;
  });
}

export function countAdjustmentsByNotes(notes: string): number {
  return withDb((db) => {
    if (!tableExists(db, 'Adjustments')) return 0;
    const row = db.prepare('SELECT COUNT(1) AS cnt FROM Adjustments WHERE Notes = ?').get(notes) as { cnt: number };
    return row?.cnt ?? 0;
  });
}

export function countHolidaysByName(name: string): number {
  return withDb((db) => {
    if (!tableExists(db, 'Holidays')) return 0;
    const row = db.prepare('SELECT COUNT(1) AS cnt FROM Holidays WHERE HolidayName = ?').get(name) as { cnt: number };
    return row?.cnt ?? 0;
  });
}

export function getHolidayByName(name: string): { holidayName: string; holidayType: string; notes: string | null } | null {
  return withDb((db) => {
    if (!tableExists(db, 'Holidays')) return null;
    const row = db
      .prepare('SELECT HolidayName AS holidayName, HolidayType AS holidayType, Notes AS notes FROM Holidays WHERE HolidayName = ?')
      .get(name) as { holidayName: string; holidayType: string; notes: string | null } | undefined;
    return row ?? null;
  });
}
