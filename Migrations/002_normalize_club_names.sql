update [SAPennantDb].[dbo].[PennantMatches]
set playerclub = 'Royal Adelaide Golf Club'
where PlayerClub = 'Royal Adelaide'

update [SAPennantDb].[dbo].[PennantMatches]
set playerclub = 'Westward HO Golf Club'
where PlayerClub in ('Westward Ho GC','Westward Ho')

update [SAPennantDb].[dbo].[PennantMatches]
set playerclub = 'Willunga Golf Club'
where PlayerClub = 'Willunga'

update [SAPennantDb].[dbo].[PennantMatches]
set playerclub = 'Mount Compass Golf Club'
where PlayerClub = 'Mount Compass'

update [SAPennantDb].[dbo].[PennantMatches]
set playerclub = 'McCracken Country Club'
where PlayerClub = 'McCracken CC'

update [SAPennantDb].[dbo].[PennantMatches]
set playerclub = 'Kooyonga Golf Club'
where PlayerClub = 'Kooyonga'

update [SAPennantDb].[dbo].[PennantMatches]
set playerclub = 'Clare Golf Club'
where PlayerClub = 'Clare'

update [SAPennantDb].[dbo].[PennantMatches]
set playerclub = 'Copperclub, The Dunes Port Hughes'
where PlayerClub = 'Copperclub, The Dunes Port Hug'

update [SAPennantDb].[dbo].[PennantMatches]
set playerclub = 'Flagstaff Hill Golf Club'
where PlayerClub = 'Flagstaff HIll'

update [SAPennantDb].[dbo].[PennantMatches]
set playerclub = 'Glenelg Golf Club'
where PlayerClub = 'GLENELG'

update [SAPennantDb].[dbo].[PennantMatches]
set playerclub = 'The Grange Golf Club (SA)'
where PlayerClub in ('Grange Golf Club','The Grange Golf Club')

UPDATE PennantMatches
SET PlayerClub = TRIM(PlayerClub)
WHERE PlayerClub != TRIM(PlayerClub);

UPDATE PennantMatches
SET OpponentClub = TRIM(OpponentClub)
WHERE OpponentClub != TRIM(OpponentClub);