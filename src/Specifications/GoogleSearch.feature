# language: es
Característica: Búsqueda en DuckDuckGo
  Como usuario de internet
  Quiero poder buscar información en DuckDuckGo
  Para encontrar contenido relevante

  @smoke @web
  Escenario: Búsqueda exitosa de Playwright
    Dado que el usuario navega al buscador
    Cuando busca el término "Playwright automation"
    Entonces debe ver resultados de búsqueda
    Y la URL debe contener "q="

  @smoke @web
  Escenario: Búsqueda de Selenium WebDriver
    Dado que el usuario navega al buscador
    Cuando busca el término "Selenium WebDriver"
    Entonces debe ver resultados de búsqueda

  @regression @web
  Esquema del escenario: Búsqueda de múltiples términos
    Dado que el usuario navega al buscador
    Cuando busca el término "<termino>"
    Entonces debe ver resultados de búsqueda

    Ejemplos:
      | termino                  |
      | Playwright               |
      | SpecFlow BDD             |
      | Clean Architecture       |
      | Screenplay Pattern       |
