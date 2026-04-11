-- Normalize pool names for consistency across seasons
UPDATE [SAPennantDb].[dbo].[PennantMatches] SET Pool = 'Women''s Cleek 1' WHERE Pool = 'Cleek 1'
UPDATE [SAPennantDb].[dbo].[PennantMatches] SET Pool = 'Women''s Cleek 2' WHERE Pool = 'Cleek 2'
UPDATE [SAPennantDb].[dbo].[PennantMatches] SET Pool = 'Sharp Cup' WHERE Pool = 'Junior Sharp Cup'