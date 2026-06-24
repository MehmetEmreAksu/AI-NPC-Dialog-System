// Bu bir class degil, bir arayuzdur (Interface).
// "Bunu kullanan herkes Interact ve GetPrompt fonksiyonlarini yazmak ZORUNDADIR" kuralini koyar.
public interface IInteractable
{
    // E'ye basilinca calisan asil etkilesim.
    void Interact();

    // Oyuncu bu objeye bakinca ekranda gosterilecek ipucu metni (orn: "[E] Al").
    string GetPrompt();
}
