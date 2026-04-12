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

  update [SAPennantDb].[dbo].[PennantMatches]
  set [OpponentClub] = 'Royal Adelaide Golf Club'
  where [OpponentClub] = 'Royal Adelaide'
  
  update [SAPennantDb].[dbo].[PennantMatches]
  set [OpponentClub] = 'Westward HO Golf Club'
  where [OpponentClub] in ('Westward Ho GC','Westward Ho')
  
  update [SAPennantDb].[dbo].[PennantMatches]
  set [OpponentClub] = 'Willunga Golf Club'
  where [OpponentClub] = 'Willunga'
  

  update [SAPennantDb].[dbo].[PennantMatches]
  set [OpponentClub] = 'Mount Compass Golf Club'
  where [OpponentClub] = 'Mount Compass'
  
  update [SAPennantDb].[dbo].[PennantMatches]
  set [OpponentClub] = 'McCracken Country Club'
  where [OpponentClub] = 'McCracken CC'

  update [SAPennantDb].[dbo].[PennantMatches]
  set [OpponentClub] = 'Kooyonga Golf Club'
  where [OpponentClub] = 'Kooyonga'

  update [SAPennantDb].[dbo].[PennantMatches]
  set [OpponentClub] = 'Clare Golf Club'
  where [OpponentClub] = 'Clare'

  update [SAPennantDb].[dbo].[PennantMatches]
  set [OpponentClub] = 'Copperclub, The Dunes Port Hughes'
  where [OpponentClub] = 'Copperclub, The Dunes Port Hug'

  update [SAPennantDb].[dbo].[PennantMatches]
  set [OpponentClub] = 'Flagstaff Hill Golf Club'
  where [OpponentClub] = 'Flagstaff HIll'

  update [SAPennantDb].[dbo].[PennantMatches]
  set [OpponentClub] = 'Glenelg Golf Club'
  where [OpponentClub] = 'GLENELG'

  update [SAPennantDb].[dbo].[PennantMatches]
  set [OpponentClub] = 'The Grange Golf Club (SA)'
  where [OpponentClub] in ('Grange Golf Club','The Grange Golf Club')