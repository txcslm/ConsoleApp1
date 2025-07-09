namespace ChubDownloader.Models;

public class CharacterInfo(string id, string name, string url, int chatCount = 0, string userName = "")
{
  public string Id { get; set; } = id;

  public string Name { get; set; } = name;

  public string Url { get; set; } = url;

  public int ChatCount { get; set; } = chatCount;

  public string UserName { get; set; } = userName;
}