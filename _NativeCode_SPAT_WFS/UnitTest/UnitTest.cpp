// UnitTest.cpp : Ce fichier contient la fonction 'main'. L'exécution du programme commence et se termine à cet endroit.
//

#include <iostream>
#include "At_SpatializationEngine.h"

using namespace Spatializer;
int main()
{
    std::cout << "Hello World!\n";
    float bufferin[2048];
    float bufferout[98304];
    At_SpatializationEngine::getInstance().WFS_initializeOutput(48000.0f, 2048, 48, 2, 100.0f);

    int id1;
    At_SpatializationEngine::getInstance().CreateWfsSpatializer(&id1, true, false, 15.0f);

    int id2;
    At_SpatializationEngine::getInstance().CreateWfsSpatializer(&id2, true, false, 15.0f);

    for (int i = 0; i < 1000; i++) {
        At_SpatializationEngine::getInstance().WFS_process(id1, bufferin, bufferout, 2048, 0, 1, 48);

        At_SpatializationEngine::getInstance().WFS_process(id2, bufferin, bufferout, 2048, 0, 1, 48);

        for (int channelIndex = 0; channelIndex < 48; channelIndex++)
        {

            for (int sampleIndex = 0; sampleIndex < 2048; sampleIndex++)
            {

                float sample = At_SpatializationEngine::getInstance().WFS_getMixingBufferSampleForChannelAndZero(sampleIndex, channelIndex, false);
            }
        }

    }
}

// Exécuter le programme : Ctrl+F5 ou menu Déboguer > Exécuter sans débogage
// Déboguer le programme : F5 ou menu Déboguer > Démarrer le débogage

// Astuces pour bien démarrer : 
//   1. Utilisez la fenêtre Explorateur de solutions pour ajouter des fichiers et les gérer.
//   2. Utilisez la fenêtre Team Explorer pour vous connecter au contrôle de code source.
//   3. Utilisez la fenêtre Sortie pour voir la sortie de la génération et d'autres messages.
//   4. Utilisez la fenêtre Liste d'erreurs pour voir les erreurs.
//   5. Accédez à Projet > Ajouter un nouvel élément pour créer des fichiers de code, ou à Projet > Ajouter un élément existant pour ajouter des fichiers de code existants au projet.
//   6. Pour rouvrir ce projet plus tard, accédez à Fichier > Ouvrir > Projet et sélectionnez le fichier .sln.
