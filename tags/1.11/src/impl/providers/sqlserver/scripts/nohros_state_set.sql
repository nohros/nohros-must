/* Checks if the current database version is greater
 * than the version of this patch.
 */
:on error exit

declare @continue bit,
  @objectname varchar(120),
  @objectversion int

set @objectname = 'nohros_state_set' /* the name of the object related with the script */
set @objectversion = 1 /* the current object version */

exec @continue = nohros_updateversion @objectname=@objectname, @objectversion=@objectversion
if @continue != 1
begin /* version guard */
  raiserror(
    'The version of the database is greater than the version of this script. The batch will be stopped', 11, 1
  )
end /* version guard */

/* create and empty procedure with the name [@pbjectname].So, we
 * can use the [alter proc [@objectname] statement]. This simulates
 * the behavior of the ALTER OR REPLACE statement that exists in other
 * datbases products. */
exec nohros_createproc @name = @objectname
go

/**
 * Copyright (c) 2011 by Nohros Inc, All rights reserved.
 */
alter proc nohros_state_set (
  @name varchar(8000),
  @state varchar(8000)
)
as

declare @state_t varchar(8000)

select @state_t = state
from nohros_state
where state_name = @name

if @state_t is null
begin
  insert into nohros_state(state_name, [state])
  values(@name, @state)
end
else
begin
  update nohros_state
  set [state] = @state
  where state_name = @name
end