open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Linq
open System.Text
open System
open Microsoft.FSharp.Reflection
open FSharp.Charting
open FSharp.Data
open Newtonsoft.Json


type Span = Span of TimeSpan with
  static member (+) (d:DateTime, Span wrapper) = d + wrapper
  static member Zero = Span(new TimeSpan(0L))

let formatDate (date:DateTime) = date.Date.ToString("yyyy-MM-dd")

let regions = [|"americas"; "europe"; "se_asia" ; "china"|]
let htmlstring (region:string) (date:string) = 
    String.concat "/" ["http://dota2toplist.com/official"; region; date]

type Player = {name: string; mmr: int; region: string; date: DateTime}


let regionDict = new Dictionary<string, Dictionary<DateTime, Player[] option>>()
regions |> Array.map (fun i -> regionDict.Add(i, new Dictionary<DateTime, Player[] option>())) |> ignore

let dateDict = new Dictionary<DateTime, Player[] option []>()


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
                        let datestring = formatDate date
                        let! results = HtmlDocument.AsyncLoad(htmlstring region datestring)
                        if (results.Descendants ["p"] |> Seq.head).InnerText().Contains("Data from " + datestring) then
                            let parsedResults = Some(parseData results region)
                            regionDict.[region].Add(date, parsedResults)
                            return parsedResults
                        else
                            printfn "%s has no data" datestring
                            regionDict.[region].Add(date, None)
                            return None}
                        ]


let scrapeOrRead = 
    let stopWatch = Stopwatch.StartNew()
    let dateDictfileName = @"dateDict.json"
    let regionDictfileName = @"regionDict.json"
    if not (File.Exists(dateDictfileName) && File.Exists(regionDictfileName)) then
        let dates = [|DateTime.Parse("2014-03-26")..Span(TimeSpan.FromDays(1.))..DateTime.Now.Date|]

        Async.Parallel [for date in dates ->
                                    async {
                                    let! result = ParseDate date
                                    printfn "Parsed %s" (formatDate date)
                                    dateDict.Add(date, result)
                                    }]
                                    |> Async.RunSynchronously
                                    |> ignore
                                   
        printfn "Parsing from online took %f ms" stopWatch.Elapsed.TotalMilliseconds

        let json_date = JsonConvert.SerializeObject(dateDict, Formatting.Indented);
        File.WriteAllText(dateDictfileName, json_date, Encoding.UTF8)
        let json_region = JsonConvert.SerializeObject(regionDict, Formatting.Indented);
        File.WriteAllText(regionDictfileName, json_region, Encoding.UTF8)
    else
        let dateDict = JsonConvert.DeserializeObject<Dictionary<DateTime, Player[] option []>>(File.OpenText(dateDictfileName).ReadToEnd())
        let regionDict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<DateTime, Player[] option>>>(File.OpenText(regionDictfileName).ReadToEnd())
        printfn "Parsing from file took %f ms" stopWatch.Elapsed.TotalMilliseconds


[<EntryPoint>]
let main argv =
    
    let Data = Array.zeroCreate(4)
    regions |> Array.iteri (fun index region -> 
                let dates = regionDict.[region].Keys.ToArray()
                let mmrs = regionDict.[region].Values.ToArray() 
                            |> Array.map (fun p -> 
                                match p with 
                                | Some i -> i |> Array.map(fun j -> float(j.mmr)) |> Array.average
                                | None -> (0.) // what to do here?
                                )
                Data.[index] <- [ for i in 0..(mmrs.Length-1) do yield (dates.[i], mmrs.[i])]
                )

    let chart = Chart.Combine(Data
                                |> Array.mapi ( fun index data -> 
                                Chart.Point (data, Name=regions.[index])
                              )).WithLegend().WithYAxis(Enabled=true, Max=8000.0, Min=4500.0)
    chart.ShowChart() |> ignore
    chart.SaveChartAs("chart.png", ChartTypes.ChartImageFormat.Png)
    0