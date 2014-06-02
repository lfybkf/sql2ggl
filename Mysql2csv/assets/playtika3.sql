select dp.name as dpname, d.name as dname, m.name as mname, h.STICKER
, ifnull(b.name, '*no buyer') as buyer, ifnull(s.name, ' ') as sname
, t.name as tname
from hardware h 
join model m on m.modelid = h.MODELID
join departm d on d.DEPWPID = h.DEPWPID
join typeequ t on t.typeequid = m.typeequid
join departm dp on dp.DEPWPID = d.parentid
left join state s on s.stateID = h.stateID
left join buyer b on b.buyerid = h.BUYERID
where d.DEPWPID in (#DEPARTM_PLAYTIKA#)
order by 1,2,3,4;