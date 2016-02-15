open System.Collections.Generic
open System.Diagnostics
open System
open Microsoft.FSharp.Reflection
open FSharp.Data


type Span = Span of TimeSpan with
  static member (+) (d:DateTime, Span wrapper) = d + wrapper
  static member Zero = Span(new TimeSpan(0L))

let formatDate (date:DateTime) = date.Date.ToString("yyyy-MM-dd")

let regions = [|"americas"; "europe"; "se_asia" ; "china"|]
let htmlstring (region:string) (date:DateTime) = 
    String.concat "/" ["http://dota2toplist.com/official"; region; formatDate date]

type Player = {name: string; mmr: int; region: string; date: DateTime}


let regionDict = new Dictionary<string, Dictionary<DateTime, Player[]>>()
regions |> Array.map (fun i -> regionDict.Add(i, new Dictionary<DateTime, Player[]>())) |> ignore

let ParseDate (date:DateTime) =
    let parseData (results:HtmlDocument) (region:string) = 
        results.Descendants ["tr"]
        |> Seq.tail
        |> Seq.map (fun i -> 
            let td = i.Descendants ["td"] |> Seq.toArray
            {name = td.[1].InnerText();
                mmr = td.[3].InnerText().AsInteger();
                region = region;
                date = date.Date
                })
        |> Seq.toArray
    
    Async.Parallel [for region in regions  ->
                        async {
                        let! results = HtmlDocument.AsyncLoad(htmlstring region date)
                        let parsedResults = parseData results region
                        regionDict.[region].Add(date, parsedResults)
                        return parsedResults}
                        ]


[<EntryPoint>]
let main argv =
    let stopWatch = Stopwatch.StartNew()
    let dates = [|DateTime.Parse("2014-03-26")..Span(TimeSpan.FromDays(1.))..DateTime.Now.Date|]
    let mmrs = Async.Parallel [for date in dates ->
                                async {
                                let! result = ParseDate date
                                printfn "Parsed %s" (formatDate date)
                                return result
                                }]
                                |> Async.RunSynchronously
    printfn "Finding took %f ms" stopWatch.Elapsed.TotalMilliseconds

    0