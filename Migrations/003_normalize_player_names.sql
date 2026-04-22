-- Date fix
UPDATE PennantMatches
SET Date = REPLACE(Date, 'Sept ', 'Sep ')
WHERE Date LIKE '%Sept%'

UPDATE PennantMatches
SET Date = REPLACE(Date, 'June ', 'Jun ')
WHERE Date LIKE '%June%'

UPDATE PennantMatches
SET Date = REPLACE(Date, 'July ', 'Jul ')
WHERE Date LIKE '%July%'

-- Including a match on player club
WITH PlayerCounts AS (
    SELECT 
        PlayerName,
        PlayerClub,
        COUNT(*) as match_count,
        MIN(Year) as first_year,
        MAX(Year) as last_year,
        -- Extract surname once
        REVERSE(SUBSTRING(REVERSE(PlayerName), 1, CHARINDEX(' ', REVERSE(PlayerName) + ' ') - 1)) as surname
    FROM PennantMatches
    GROUP BY PlayerName, PlayerClub
)
SELECT 
    a.PlayerName as name1,
    a.match_count as name1_count,
    a.first_year as name1_first_year,
    a.last_year as name1_last_year,
    b.PlayerName as name2,
    b.match_count as name2_count,
    b.first_year as name2_first_year,
    b.last_year as name2_last_year,
    a.PlayerClub as club
FROM PlayerCounts a
JOIN PlayerCounts b
    ON a.PlayerClub = b.PlayerClub
    AND a.PlayerName < b.PlayerName
    AND a.surname = b.surname
    AND LEFT(a.PlayerName, 1) = LEFT(b.PlayerName, 1)
ORDER BY a.PlayerClub, a.PlayerName

-- Excluding a match on player club
WITH PlayerCounts AS (
    SELECT 
        PlayerName,
        PlayerClub,
        COUNT(*) as match_count,
        MIN(Year) as first_year,
        MAX(Year) as last_year,
        REVERSE(SUBSTRING(REVERSE(PlayerName), 1, CHARINDEX(' ', REVERSE(PlayerName) + ' ') - 1)) as surname
    FROM PennantMatches
    GROUP BY PlayerName, PlayerClub
)
SELECT 
    a.PlayerName as name1,
    a.PlayerClub as name1_club,
    a.match_count as name1_count,
    a.first_year as name1_first_year,
    a.last_year as name1_last_year,
    b.PlayerName as name2,
    b.PlayerClub as name2_club,
    b.match_count as name2_count,
    b.first_year as name2_first_year,
    b.last_year as name2_last_year
FROM PlayerCounts a
JOIN PlayerCounts b
    ON a.PlayerName < b.PlayerName
    AND a.surname = b.surname
    AND LEFT(a.PlayerName, 1) = LEFT(b.PlayerName, 1)
ORDER BY a.PlayerName


UPDATE PennantMatches SET PlayerName = 'Don Payne' WHERE PlayerName = 'Donald Payne' AND PlayerClub ='Aston Hills Golf Club at Mount Barker'; UPDATE PennantMatches SET OpponentName = 'Don Payne' WHERE OpponentName = 'Donald Payne' AND OpponentClub = 'Aston Hills Golf Club at Mount Barker';
UPDATE PennantMatches SET PlayerName = 'Rod Short' WHERE PlayerName = 'Rodney Short' AND PlayerClub ='Aston Hills Golf Club at Mount Barker'; UPDATE PennantMatches SET OpponentName = 'Rod Short' WHERE OpponentName = 'Rodney Short' AND OpponentClub = 'Aston Hills Golf Club at Mount Barker';
UPDATE PennantMatches SET PlayerName = 'Geoff Hunter' WHERE PlayerName = 'Geoffrey Hunter' AND PlayerClub ='Barossa Valley Golf Club'; UPDATE PennantMatches SET OpponentName = 'Geoff Hunter' WHERE OpponentName = 'Geoffrey Hunter' AND OpponentClub = 'Barossa Valley Golf Club';
UPDATE PennantMatches SET PlayerName = 'Indiana Danger' WHERE PlayerName = 'Indie Danger' AND PlayerClub ='Glenelg Golf Club'; UPDATE PennantMatches SET OpponentName = 'Indiana Danger' WHERE OpponentName = 'Indie Danger' AND OpponentClub = 'Glenelg Golf Club';
UPDATE PennantMatches SET PlayerName = 'Jackson Leonard' WHERE PlayerName = 'Jack Leonard' AND PlayerClub ='Glenelg Golf Club'; UPDATE PennantMatches SET OpponentName = 'Jackson Leonard' WHERE OpponentName = 'Jack Leonard' AND OpponentClub = 'Glenelg Golf Club';
UPDATE PennantMatches SET PlayerName = 'Matt Uebergang' WHERE PlayerName = 'Matthew Uebergang' AND PlayerClub ='Glenelg Golf Club'; UPDATE PennantMatches SET OpponentName = 'Matt Uebergang' WHERE OpponentName = 'Matthew Uebergang' AND OpponentClub = 'Glenelg Golf Club';
UPDATE PennantMatches SET PlayerName = 'Mick Phillips' WHERE PlayerName = 'Michael Phillips' AND PlayerClub ='Glenelg Golf Club'; UPDATE PennantMatches SET OpponentName = 'Mick Phillips' WHERE OpponentName = 'Michael Phillips' AND OpponentClub = 'Glenelg Golf Club';
UPDATE PennantMatches SET PlayerName = 'Shakira-Ann Kuys' WHERE PlayerName = 'Shakira Kuys' AND PlayerClub ='Glenelg Golf Club'; UPDATE PennantMatches SET OpponentName = 'Shakira-Ann Kuys' WHERE OpponentName = 'Shakira Kuys' AND OpponentClub = 'Glenelg Golf Club';
UPDATE PennantMatches SET PlayerName = 'Lita-Kisun Ki Sun Nam' WHERE PlayerName = 'Lita - Ki Sun Ki Sun Nam' AND PlayerClub ='North Adelaide Golf Club'; UPDATE PennantMatches SET OpponentName = 'Lita-Kisun Ki Sun Nam' WHERE OpponentName = 'Lita - Ki Sun Ki Sun Nam' AND OpponentClub = 'North Adelaide Golf Club';
UPDATE PennantMatches SET PlayerName = 'Mihyun Park' WHERE PlayerName = 'Mi Park' AND PlayerClub ='North Adelaide Golf Club'; UPDATE PennantMatches SET OpponentName = 'Mihyun Park' WHERE OpponentName = 'Mi Park' AND OpponentClub = 'North Adelaide Golf Club';
UPDATE PennantMatches SET PlayerName = 'Seâ Young Jung' WHERE PlayerName = 'Se Jung' AND PlayerClub ='North Adelaide Golf Club'; UPDATE PennantMatches SET OpponentName = 'Seâ Young Jung' WHERE OpponentName = 'Se Jung' AND OpponentClub = 'North Adelaide Golf Club';
UPDATE PennantMatches SET PlayerName = 'Yungâ Bok Kim' WHERE PlayerName = 'Yung Bok Kim' AND PlayerClub ='North Adelaide Golf Club'; UPDATE PennantMatches SET OpponentName = 'Yungâ Bok Kim' WHERE OpponentName = 'Yung Bok Kim' AND OpponentClub = 'North Adelaide Golf Club';
UPDATE PennantMatches SET PlayerName = 'Yungâ Bok Kim' WHERE PlayerName = 'Yungbok Kim' AND PlayerClub ='North Adelaide Golf Club'; UPDATE PennantMatches SET OpponentName = 'Yungâ Bok Kim' WHERE OpponentName = 'Yungbok Kim' AND OpponentClub = 'North Adelaide Golf Club';
UPDATE PennantMatches SET PlayerName = 'Szymon Gomulka Domagala' WHERE PlayerName = 'Szymon Domagala' AND PlayerClub ='North Haven Golf Club'; UPDATE PennantMatches SET OpponentName = 'Szymon Gomulka Domagala' WHERE OpponentName = 'Szymon Domagala' AND OpponentClub = 'North Haven Golf Club';
UPDATE PennantMatches SET PlayerName = 'Stephen Handley' WHERE PlayerName = 'Steve Handley' AND PlayerClub ='Penfield Golf Club'; UPDATE PennantMatches SET OpponentName = 'Stephen Handley' WHERE OpponentName = 'Steve Handley' AND OpponentClub = 'Penfield Golf Club';
UPDATE PennantMatches SET PlayerName = 'Alston Ma' WHERE PlayerName = 'Alston (Zi Mu) Ma' AND PlayerClub ='Royal Adelaide Golf Club'; UPDATE PennantMatches SET OpponentName = 'Alston Ma' WHERE OpponentName = 'Alston (Zi Mu) Ma' AND OpponentClub = 'Royal Adelaide Golf Club';
UPDATE PennantMatches SET PlayerName = 'Rod Phillips' WHERE PlayerName = 'Rodney Phillips' AND PlayerClub ='Royal Adelaide Golf Club'; UPDATE PennantMatches SET OpponentName = 'Rod Phillips' WHERE OpponentName = 'Rodney Phillips' AND OpponentClub = 'Royal Adelaide Golf Club';
UPDATE PennantMatches SET PlayerName = 'Chris Hayward' WHERE PlayerName = 'Chris Jf Hayward' AND PlayerClub ='South Lakes Golf Club'; UPDATE PennantMatches SET OpponentName = 'Chris Hayward' WHERE OpponentName = 'Chris Jf Hayward' AND OpponentClub = 'South Lakes Golf Club';
UPDATE PennantMatches SET PlayerName = 'David Pink Ovens' WHERE PlayerName = 'David Ovens' AND PlayerClub ='Tea Tree Gully Golf Club'; UPDATE PennantMatches SET OpponentName = 'David Pink Ovens' WHERE OpponentName = 'David Ovens' AND OpponentClub = 'Tea Tree Gully Golf Club';
UPDATE PennantMatches SET PlayerName = 'Nicholas Oborn' WHERE PlayerName = 'Nick Oborn' AND PlayerClub ='The Grange Golf Club (SA)'; UPDATE PennantMatches SET OpponentName = 'Nicholas Oborn' WHERE OpponentName = 'Nick Oborn' AND OpponentClub = 'The Grange Golf Club (SA)';
UPDATE PennantMatches SET PlayerName = 'Andrew Stoodley' WHERE PlayerName = 'Andrew James Stoodley' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Andrew Stoodley' WHERE OpponentName = 'Andrew James Stoodley' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Ava Strazdins' WHERE PlayerName = 'Ava Jay Strazdins' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Ava Strazdins' WHERE OpponentName = 'Ava Jay Strazdins' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Brendan Fisher' WHERE PlayerName = 'Brendan James Fisher' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Brendan Fisher' WHERE OpponentName = 'Brendan James Fisher' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Catherine Hayward' WHERE PlayerName = 'Catherine Dianne Hayward' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Catherine Hayward' WHERE OpponentName = 'Catherine Dianne Hayward' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Charlie Nobbs' WHERE PlayerName = 'Charlie Brenden Nobbs' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Charlie Nobbs' WHERE OpponentName = 'Charlie Brenden Nobbs' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Christopher Burns' WHERE PlayerName = 'Christopher Michael Burns' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Christopher Burns' WHERE OpponentName = 'Christopher Michael Burns' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Denise Walters' WHERE PlayerName = 'Denise Mary Walters' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Denise Walters' WHERE OpponentName = 'Denise Mary Walters' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Emma Hastie' WHERE PlayerName = 'Emma Ingrid Hastie' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Emma Hastie' WHERE OpponentName = 'Emma Ingrid Hastie' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Harrison Percey' WHERE PlayerName = 'Harrison Luke Percey' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Harrison Percey' WHERE OpponentName = 'Harrison Luke Percey' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'James Hastie' WHERE PlayerName = 'James Steven Hastie' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'James Hastie' WHERE OpponentName = 'James Steven Hastie' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Jennifer Hirst' WHERE PlayerName = 'Jennifer Anne Hirst' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Jennifer Hirst' WHERE OpponentName = 'Jennifer Anne Hirst' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Jordan Rhys Percey' WHERE PlayerName = 'Jordan Percey' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Jordan Rhys Percey' WHERE OpponentName = 'Jordan Percey' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Kathryn Michaelsen' WHERE PlayerName = 'Kathryn Yvonne Michaelsen' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Kathryn Michaelsen' WHERE OpponentName = 'Kathryn Yvonne Michaelsen' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Lynnette Cummings' WHERE PlayerName = 'Lynnette Margaret Cummings' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Lynnette Cummings' WHERE OpponentName = 'Lynnette Margaret Cummings' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Marc Fulton' WHERE PlayerName = 'Marc Alan Fulton' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Marc Fulton' WHERE OpponentName = 'Marc Alan Fulton' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Mary O''hagan' WHERE PlayerName = 'Mary Elizabeth O''hagan' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Mary O''hagan' WHERE OpponentName = 'Mary Elizabeth O''hagan' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Michael Patrick Bruggeman' WHERE PlayerName = 'Michael Bruggeman' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Michael Patrick Bruggeman' WHERE OpponentName = 'Michael Bruggeman' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Olivia Nobbs' WHERE PlayerName = 'Olivia Dawn Nobbs' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Olivia Nobbs' WHERE OpponentName = 'Olivia Dawn Nobbs' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Simon Lohmeyer' WHERE PlayerName = 'Simon James Lohmeyer' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Simon Lohmeyer' WHERE OpponentName = 'Simon James Lohmeyer' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Tony Nobbs' WHERE PlayerName = 'Tony Brendan Nobbs' AND PlayerClub ='The Vines Golf Club of Reynella'; UPDATE PennantMatches SET OpponentName = 'Tony Nobbs' WHERE OpponentName = 'Tony Brendan Nobbs' AND OpponentClub = 'The Vines Golf Club of Reynella';
UPDATE PennantMatches SET PlayerName = 'Isaac Hore' WHERE PlayerName = 'Issac Hore' AND PlayerClub ='Westward HO Golf Club'; UPDATE PennantMatches SET OpponentName = 'Isaac Hore' WHERE OpponentName = 'Issac Hore' AND OpponentClub = 'Westward HO Golf Club';
UPDATE PennantMatches SET PlayerName = 'Matt Hage' WHERE PlayerName = 'Matthew Hage' AND PlayerClub ='Willunga Golf Club'; UPDATE PennantMatches SET OpponentName = 'Matt Hage' WHERE OpponentName = 'Matthew Hage' AND OpponentClub = 'Willunga Golf Club';

UPDATE PennantMatches SET PlayerName = 'Harry Percey' WHERE PlayerName = 'Harrison Percey'; UPDATE PennantMatches SET OpponentName = 'Harry Percey' WHERE OpponentName = 'Harrison Percey';
UPDATE PennantMatches SET PlayerName = 'Jonathan Bowen' WHERE PlayerName = 'Jonathan Paul Bowen'; UPDATE PennantMatches SET OpponentName = 'Jonathan Bowen' WHERE OpponentName = 'Jonathan Paul Bowen';
UPDATE PennantMatches SET PlayerName = 'Jordan Percey' WHERE PlayerName = 'Jordan Rhys Percey'; UPDATE PennantMatches SET OpponentName = 'Jordan Percey' WHERE OpponentName = 'Jordan Rhys Percey';
UPDATE PennantMatches SET PlayerName = 'Josh Russo' WHERE PlayerName = 'Joshua Russo'; UPDATE PennantMatches SET OpponentName = 'Josh Russo' WHERE OpponentName = 'Joshua Russo';
UPDATE PennantMatches SET PlayerName = 'Kui Liu' WHERE PlayerName = 'Kui Lisa Liu'; UPDATE PennantMatches SET OpponentName = 'Kui Liu' WHERE OpponentName = 'Kui Lisa Liu';
UPDATE PennantMatches SET PlayerName = 'Mick Phillips' WHERE PlayerName = 'Michael Phillips'; UPDATE PennantMatches SET OpponentName = 'Mick Phillips' WHERE OpponentName = 'Michael Phillips';
UPDATE PennantMatches SET PlayerName = 'Soo Lee' WHERE PlayerName = 'Sunjeo Lee'; UPDATE PennantMatches SET OpponentName = 'Soo Lee' WHERE OpponentName = 'Sunjeo Lee';

SELECT DISTINCT "PlayerName", "PlayerClub", COUNT(*) as cnt
FROM "PennantMatches"
WHERE "PlayerName" LIKE 'Yung%Kim'
GROUP BY "PlayerName", "PlayerClub"

UPDATE "PennantMatches"
SET "PlayerName" = 'Yung Bok Kim'
WHERE "PlayerName"::bytea = '\x59756e67c3a2c2a0426f6b204b696d'::bytea
   OR "PlayerName"::bytea = '\x59756e67c3a220426f6b204b696d'::bytea;

UPDATE "PennantMatches"
SET "OpponentName" = 'Yung Bok Kim'
WHERE "OpponentName"::bytea = '\x59756e67c3a2c2a0426f6b204b696d'::bytea
   OR "OpponentName"::bytea = '\x59756e67c3a220426f6b204b696d'::bytea;


   SELECT DISTINCT "PlayerName", encode("PlayerName"::bytea, 'hex') as hex_value
FROM "PennantMatches"
WHERE "PlayerName" LIKE 'Se%Jung'

UPDATE "PennantMatches"
SET "PlayerName" = 'Se Young Jung'
WHERE "PlayerName"::bytea = '\x5365c3a2c2a0596f756e67204a756e67'::bytea
   OR "PlayerName"::bytea = '\x5365c3a220596f756e67204a756e67'::bytea;

UPDATE "PennantMatches"
SET "OpponentName" = 'Se Young Jung'
WHERE "OpponentName"::bytea = '\x5365c3a2c2a0596f756e67204a756e67'::bytea
   OR "OpponentName"::bytea = '\x5365c3a220596f756e67204a756e67'::bytea;