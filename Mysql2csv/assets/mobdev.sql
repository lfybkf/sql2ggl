select d.depwpid, d.parentid, d.name
from departm d
where d.depwpid in (#DEPARTM_PLAYTIKA#)
order by 1,3;
