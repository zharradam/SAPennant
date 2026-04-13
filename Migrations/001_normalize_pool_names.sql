-- Normalize pool names for consistency across seasons
UPDATE [SAPennantDb].[dbo].[PennantMatches] SET Pool = 'Div 1' WHERE Pool = 'Division 1'
UPDATE [SAPennantDb].[dbo].[PennantMatches] SET Pool = 'Div 2' WHERE Pool = 'Division 2'
UPDATE [SAPennantDb].[dbo].[PennantMatches] SET Pool = 'Div 3' WHERE Pool = 'Division 3'
UPDATE [SAPennantDb].[dbo].[PennantMatches] SET Pool = 'Women''s Cleek 1' WHERE Pool = 'Cleek 1'
UPDATE [SAPennantDb].[dbo].[PennantMatches] SET Pool = 'Women''s Cleek 2' WHERE Pool = 'Cleek 2'
UPDATE [SAPennantDb].[dbo].[PennantMatches] SET Pool = 'Sharp Cup' WHERE Pool = 'Junior Sharp Cup'
UPDATE [SAPennantDb].[dbo].[PennantMatches] SET Awayclub = 'Country Districts' WHERE Awayclub = 'Country District'