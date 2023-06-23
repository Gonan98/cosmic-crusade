using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace RTSEngine
{
    public class GridObject
    {
        private int x;
        private int z;
        private float cellSize;

        private bool isBusy;
        private bool isActive;

        private GameObject statusTile;
        public Collider tileBoundaryCollider { private set; get; } //this is the collider that define the building's zone on the map where other buildings are not allowed to be placed.
        public GridObjectItem placedObject;

        public GridObject(int x, int z, float cellSize, bool isBusy = false, bool isActive = false)
        {
            this.x = x;
            this.z = z;
            this.cellSize = cellSize;
            this.isBusy = isBusy;
            this.isActive = isActive;
            this.placedObject = null;
            this.statusTile = null;
            this.tileBoundaryCollider = null;
            // this.isBusy = this.isTileOnMap(gameMgr) || busy;

        }

        public override string ToString()
        {
            return x + ", " + z + ", " + isBusy;
        }
        public Vector2Int GetGridPosition()
        {
            return new Vector2Int(x, z);
        }
        public void SetPlacedObject(GridObjectItem placedObject)
        {
            this.placedObject = placedObject;
            
            this.isBusy = placedObject != null;
        }

        public void ClearPlacedObject()
        {
            placedObject = null;
            this.statusTile.SetActive(false);
            this.tileBoundaryCollider = null;
            this.isBusy = false;
        }

        public GridObjectItem GetPlacedObject()
        {
            return placedObject;
        }

        public bool CanBuild()
        {
            return placedObject == null && !isBusy;
        }
        public GameObject GetStatusTile()
        {
            return statusTile;
        }
        public void SetStatusTile(GameObject statusTile)
        {
            this.statusTile = statusTile;
            setBusyState();
        }
        public bool GetIsActive()
        {
            return isActive;
        }
        public void SetActive(bool isActive)
        {
            this.isActive = isActive;
            this.statusTile.SetActive(isActive);
        }
        public bool GetIsBusy()
        {
            return isBusy;
        }
        public void SetIsBusy(bool isBusy)
        {
            this.isBusy = isBusy;
        }
        public bool isTileOnMap(GameManager gameMgr)
        {
            Ray ray = new Ray(); //create a new ray
            RaycastHit[] hits; //this will hold the registerd hits by the above ray
            BoxCollider boxCollider = statusTile.GetComponent<BoxCollider>();
            Transform transform = statusTile.transform;

            //Start by checking if the middle point of the building's collider is over the map.
            //Set the ray check source point which is the center of the collider in the game world:
            ray.origin = new Vector3(transform.position.x + boxCollider.center.x, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z);

            ray.direction = Vector3.down; //The direction of the ray is always down because we want check if there's terrain right under the building's object:

            int i = 4; //we will check the four corners and the center
            while (i > 0) //as long as the building is still on the map/terrain
            {
                hits = Physics.RaycastAll(ray, 1f); //apply the raycast and store the hits
                bool hitTerrain = false; //did one the hits hit the terrain?
                foreach (RaycastHit rh in hits)
                { //go through all hits
                    // layerId = rh.transform.gameObject.layer;
                    if (gameMgr.TerrainMgr.IsTerrainTile(rh.transform.gameObject) || rh.transform.gameObject.layer == 11) //is this a terrain object?
                        hitTerrain = true;
                }
                if (hitTerrain == false) //if there was no registerd terrain hit
                    return false; //stop and return false

                i--;

                //If we reached this stage, then applying the last raycast, we successfully detected that there was a terrain under it, so we'll move to the next corner:
                switch (i)
                {
                    case 0:
                        ray.origin = new Vector3(transform.position.x + boxCollider.center.x + boxCollider.size.x / 2, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z + boxCollider.size.z / 2);
                        break;
                    case 1:
                        ray.origin = new Vector3(transform.position.x + boxCollider.center.x + boxCollider.size.x / 2, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z - boxCollider.size.z / 2);
                        break;
                    case 2:
                        ray.origin = new Vector3(transform.position.x + boxCollider.center.x - boxCollider.size.x / 2, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z - boxCollider.size.z / 2);
                        break;
                    case 3:
                        ray.origin = new Vector3(transform.position.x + boxCollider.center.x - boxCollider.size.x / 2, transform.position.y + 0.5f, transform.position.z + boxCollider.center.z + boxCollider.size.z / 2);
                        break;
                }
            }

            return true; //at this stage, we're sure that the center and all corners of the building are on the map, so return true
        }
        private void UpdateTileStatus(bool isBuildingStatusTile, bool isTileOnMap = true)
        {
            statusTile.tag = "StatusTile";
            if (CanBuild() && isTileOnMap && !isBusy)
            {
                if (isBuildingStatusTile)
                {
                    Renderer[] renderers = statusTile.GetComponentsInChildren<Renderer>();
                    renderers[0].material.SetColor("_Color", new Color(0, 255, 0, 0.3f));
                }
                else
                {
                    Renderer[] renderers = statusTile.GetComponentsInChildren<Renderer>();
                    renderers[0].material.SetColor("_Color", new Color(200, 200, 200, 0.1f));
                }

            }
            else
            {
                if (isBuildingStatusTile)
                {
                    Renderer[] renderers = statusTile.GetComponentsInChildren<Renderer>();
                    renderers[0].material.SetColor("_Color", new Color(255, 0, 0, 0.3f));
                    statusTile.tag = "BusyStatusTile";
                }
                else
                {
                    Renderer[] renderers = statusTile.GetComponentsInChildren<Renderer>();
                    renderers[0].material.SetColor("_Color", new Color(255, 255, 0, 0.3f));
                }
            }

        }
        public void UpdateTile(float nextBuildingPosY, bool isBuildingStatusTile, GameManager gameMgr, bool isActive = true)
        {
            SetActive(isActive);
            Vector3 newPosition = new Vector3(statusTile.transform.position.x, nextBuildingPosY + 0.01f, statusTile.transform.position.z);
            statusTile.transform.position = newPosition;
            UpdateTileStatus(isBuildingStatusTile, this.isTileOnMap(gameMgr));

        }
        public void setBusyState()
        {
            Ray ray = new Ray(); //create a new ray
            RaycastHit[] hits; //this will hold the registerd hits by the above ray
            BoxCollider boxCollider = statusTile.GetComponent<BoxCollider>();
            Transform transform = statusTile.transform;

            //Start by checking if the middle point of the building's collider is over the map.
            //Set the ray check source point which is the center of the collider in the game world:
            ray.origin = new Vector3(transform.position.x + boxCollider.center.x, transform.position.y + 100f, transform.position.z + boxCollider.center.z);

            ray.direction = Vector3.down; //The direction of the ray is always down because we want check if there's terrain right under the building's object:

            hits = Physics.RaycastAll(ray, 200f); //apply the raycast and store the hits
            int layerId = 0;
            this.isBusy = false;
            foreach (RaycastHit rh in hits)
            { //go through all hits
                layerId = rh.transform.gameObject.layer;
                // layerId = rh.transform.gameObject.layer;
                if (layerId == 11 || layerId == 10 || layerId == 9) //is this a terrain object?
                    this.isBusy = true;
            }


        }
    }
}