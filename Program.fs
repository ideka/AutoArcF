open System
open System.Net
open System.IO
open System.Security.Cryptography
open System.Diagnostics

let localPath = "bin64/"
let localBakPath = "bin64/bak/"
let gameExeName = "Gw2-64.exe"

type Remote = {
    name: string
    remotePath: string
    checksumPath: string option
    overrideName: string option
}
with
    member remote.localName =
        match remote.overrideName with
        | Some value -> value
        | None -> remote.remotePath.Split([|'/'|]) |> Array.last

    member remote.fullLocalPath = localPath + remote.localName
    member remote.fullBakPath = localBakPath + remote.localName
end

module Remote = 
    let updateExists (remote: Remote) =
        printfn "Checking for %s updates..." remote.name
        if not <| File.Exists(remote.fullLocalPath) then
            printfn "Local file not found, downloading..."
            true
        else
            use resp = WebRequest.Create(remote.remotePath).GetResponse() :?> HttpWebResponse
            let lastModifiedRepr = resp.LastModified.ToString()
            if File.GetLastWriteTimeUtc(remote.fullLocalPath) >= resp.LastModified then
                printfn "Up to date (%s)." lastModifiedRepr
                false
            else
                printfn "Downloading update (%s)..." lastModifiedRepr
                Directory.CreateDirectory(localBakPath) |> ignore
                File.Copy(remote.fullLocalPath, remote.fullBakPath, true)
                true

    let tryDownload (remote: Remote) =
        use wc = new WebClient()
        let file = wc.DownloadData(remote.remotePath)
        match remote.checksumPath with
        | None ->
            printfn "No checksum verification for %s." remote.name
            Some file
        | Some checksumPath ->
            use md5 = MD5.Create()
            let checksum = wc.DownloadString(checksumPath).Split([|' '|]) |> Array.head |> String.filter (not << Char.IsWhiteSpace)
            let fileChecksum = file |> md5.ComputeHash |> BitConverter.ToString |> String.filter (fun c -> c <> '-') |> String.map Char.ToLower
            let equal = checksum = fileChecksum
            printfn "%s == %s (%b)" checksum fileChecksum equal
            if not <| equal then
                Console.ReadKey() |> ignore
                None
            else
                Some file

    let update (remote: Remote) =
        if updateExists remote then
            match tryDownload remote with
            | None -> ()
            | Some file ->
                File.WriteAllBytes(remote.fullLocalPath, file)

        printfn ""

[<EntryPoint>]
let main argv =
    Remote.update {
        name = "ArcDPS"
        remotePath = "https://www.deltaconnected.com/arcdps/x64/d3d9.dll"
        checksumPath = Some "https://www.deltaconnected.com/arcdps/x64/d3d9.dll.md5sum"
        overrideName = None
    }

    Remote.update {
        name = "ArcDPS Build Templates"
        remotePath = "https://www.deltaconnected.com/arcdps/x64/buildtemplates/d3d9_arcdps_buildtemplates.dll"
        checksumPath = None
        overrideName = None
    }

    Remote.update {
        name = "ArcDPS Extras"
        remotePath = "https://www.deltaconnected.com/arcdps/x64/extras/d3d9_arcdps_extras.dll"
        checksumPath = None
        overrideName = None
    }

    Remote.update {
        name = "ArcDPS Mechanics"
        remotePath = "http://martionlabs.com/wp-content/uploads/d3d9_arcdps_mechanics.dll"
        checksumPath = Some "http://martionlabs.com/wp-content/uploads/d3d9_arcdps_mechanics.dll.md5sum"
        overrideName = None
    }

    match Array.tryLast argv with
    | None -> Process.Start(gameExeName)
    | Some arg -> Process.Start(gameExeName, arg)
    |> ignore

    0
